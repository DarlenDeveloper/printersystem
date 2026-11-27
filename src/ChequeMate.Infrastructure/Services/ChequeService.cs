using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using ChequeMate.Core.Services;
using ChequeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace ChequeMate.Infrastructure.Services;

public class ChequeService : IChequeService
{
    private readonly AppDbContext _db;
    private readonly IAmountToWordsService _amountToWords;

    public ChequeService(AppDbContext db, IAmountToWordsService amountToWords)
    {
        _db = db;
        _amountToWords = amountToWords;
    }

    public async Task<List<ChequeEntry>> GetPendingEntriesAsync()
    {
        return await _db.ChequeEntries
            .Include(e => e.Template)
                .ThenInclude(t => t!.Fields)
            .Include(e => e.Template)
                .ThenInclude(t => t!.BankProfile)
            .Include(e => e.PrintHistories)
            .Where(e => e.PrintStatus != PrintStatus.Printed)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ChequeEntry>> GetAllEntriesAsync()
    {
        return await _db.ChequeEntries
            .Include(e => e.Template)
                .ThenInclude(t => t!.Fields)
            .Include(e => e.Template)
                .ThenInclude(t => t!.BankProfile)
            .Include(e => e.PrintHistories)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChequeEntry?> GetEntryByIdAsync(int id)
    {
        return await _db.ChequeEntries
            .Include(e => e.Template)
            .ThenInclude(t => t!.Fields)
            .Include(e => e.Template)
            .ThenInclude(t => t!.BankProfile)
            .Include(e => e.PrintHistories)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<ChequeEntry> CreateEntryAsync(ChequeEntry entry)
    {
        if (string.IsNullOrEmpty(entry.AmountWords))
        {
            var template = await _db.ChequeTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == entry.TemplateId)
                ?? throw new InvalidOperationException("The selected cheque preset no longer exists.");
            EnsureTemplateEnabled(template);
            entry.AmountWords = ConvertAmount(entry.AmountFigures, template);
        }

        entry.CreatedAt = DateTime.UtcNow;
        _db.ChequeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task UpdateEntryAsync(ChequeEntry entry)
    {
        _db.ChequeEntries.Update(entry);
        await _db.SaveChangesAsync();
    }

    public async Task ConfirmPrintedAsync(int id)
    {
        var entry = await _db.ChequeEntries.FindAsync(id)
            ?? throw new InvalidOperationException("Cheque entry was not found.");

        if (entry.PrintStatus != PrintStatus.Submitted)
            throw new InvalidOperationException("Only a submitted print job can be confirmed as printed.");

        entry.PrintStatus = PrintStatus.Printed;
        entry.PrintedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RequeueAsync(int id)
    {
        var entry = await _db.ChequeEntries.FindAsync(id)
            ?? throw new InvalidOperationException("Cheque entry was not found.");

        if (entry.PrintStatus == PrintStatus.Printed)
            throw new InvalidOperationException("A confirmed printed cheque cannot be requeued.");

        entry.PrintStatus = PrintStatus.Pending;
        entry.PrintedAt = null;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteEntryAsync(int id)
    {
        var entry = await _db.ChequeEntries.FindAsync(id);
        if (entry != null)
        {
            _db.ChequeEntries.Remove(entry);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<ChequeEntry>> ImportFromCsvAsync(string filePath, int templateId)
    {
        var entries = new List<ChequeEntry>();
        var lines = await File.ReadAllLinesAsync(filePath);
        var template = await _db.ChequeTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == templateId)
            ?? throw new InvalidOperationException("The selected cheque preset no longer exists.");
        EnsureTemplateEnabled(template);

        // Expect CSV format: Payee,Amount,Date,ChequeNumber,Memo
        foreach (var line in lines.Skip(1)) // skip header
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 2) continue;

            var payee = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(payee)) continue;
            if (!decimal.TryParse(parts[1].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ||
                amount <= 0) continue;

            var date = parts.Length > 2 && DateTime.TryParse(parts[2].Trim(), CultureInfo.InvariantCulture, out var d)
                ? d : DateTime.Today;
            var chequeNumber = parts.Length > 3 ? NullIfWhiteSpace(parts[3]) : null;
            var memo = parts.Length > 4 ? NullIfWhiteSpace(parts[4]) : null;

            var entry = new ChequeEntry
            {
                TemplateId = templateId,
                Payee = payee,
                AmountFigures = amount,
                AmountWords = ConvertAmount(amount, template),
                ChequeDate = date,
                ChequeNumber = chequeNumber,
                Memo = memo
            };

            entries.Add(entry);
        }

        _db.ChequeEntries.AddRange(entries);
        await _db.SaveChangesAsync();
        return entries;
    }

    internal static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var insideQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (character == ',' && !insideQuotes)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        values.Add(value.ToString());
        return values.ToArray();
    }

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string ConvertAmount(decimal amount, ChequeTemplate template) =>
        _amountToWords.Convert(
            amount,
            template.IncludeCurrencyInWords ? template.CurrencyName : string.Empty,
            template.FractionalUnitName);

    private static void EnsureTemplateEnabled(ChequeTemplate template)
    {
        if (!template.IsEnabled)
        {
            throw new InvalidOperationException(
                "This cheque preset has been retired. Choose a current specimen-backed preset.");
        }
    }
}
