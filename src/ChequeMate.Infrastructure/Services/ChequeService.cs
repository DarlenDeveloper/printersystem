using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using ChequeMate.Core.Services;
using ChequeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            .Where(e => e.PrintStatus == PrintStatus.Pending)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ChequeEntry>> GetAllEntriesAsync()
    {
        return await _db.ChequeEntries
            .Include(e => e.Template)
                .ThenInclude(t => t!.Fields)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChequeEntry?> GetEntryByIdAsync(int id)
    {
        return await _db.ChequeEntries
            .Include(e => e.Template)
            .ThenInclude(t => t!.Fields)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<ChequeEntry> CreateEntryAsync(ChequeEntry entry)
    {
        if (string.IsNullOrEmpty(entry.AmountWords))
            entry.AmountWords = _amountToWords.Convert(entry.AmountFigures);

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

        // Expect CSV format: Payee,Amount,Date,ChequeNumber,Memo
        foreach (var line in lines.Skip(1)) // skip header
        {
            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            var payee = parts[0].Trim().Trim('"');
            if (!decimal.TryParse(parts[1].Trim().Trim('"'), out var amount)) continue;

            var date = parts.Length > 2 && DateTime.TryParse(parts[2].Trim().Trim('"'), out var d)
                ? d : DateTime.Today;
            var chequeNumber = parts.Length > 3 ? parts[3].Trim().Trim('"') : null;
            var memo = parts.Length > 4 ? parts[4].Trim().Trim('"') : null;

            var entry = new ChequeEntry
            {
                TemplateId = templateId,
                Payee = payee,
                AmountFigures = amount,
                AmountWords = _amountToWords.Convert(amount),
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
}
