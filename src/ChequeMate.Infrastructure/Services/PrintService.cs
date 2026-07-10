using System.Drawing.Printing;
using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using ChequeMate.Core.Services;
using ChequeMate.Infrastructure.Data;
using ChequeMate.Infrastructure.Printing;

namespace ChequeMate.Infrastructure.Services;

public class PrintService : IPrintService
{
    private readonly AppDbContext _db;
    private readonly ChequePrintEngine _engine;

    public PrintService(AppDbContext db, ChequePrintEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    public async Task<PrintHistory> PrintChequeAsync(ChequeEntry entry, string? printerName = null)
    {
        var template = entry.Template;
        if (template == null)
            throw new InvalidOperationException("Cheque entry must have a loaded template.");

        var history = new PrintHistory
        {
            ChequeEntryId = entry.Id,
            PrinterName = printerName
        };

        try
        {
            entry.PrintStatus = PrintStatus.Printing;

            var printDoc = new PrintDocument();
            if (!string.IsNullOrEmpty(printerName))
                printDoc.PrinterSettings.PrinterName = printerName;

            // Set custom paper size matching the cheque dimensions
            var paperSize = PaperSizeHelper.GetOrCreatePaperSize(
                printDoc.PrinterSettings, template.PaperWidthMm, template.PaperHeightMm);
            printDoc.DefaultPageSettings.PaperSize = paperSize;
            printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            // Cheques are landscape if wider than tall
            printDoc.DefaultPageSettings.Landscape =
                template.PaperWidthMm > template.PaperHeightMm;

            printDoc.PrintPage += (sender, e) =>
            {
                if (e.Graphics != null)
                    _engine.RenderCheque(e.Graphics, entry, template);
                e.HasMorePages = false;
            };

            await Task.Run(() => printDoc.Print());

            entry.PrintStatus = PrintStatus.Printed;
            entry.PrintedAt = DateTime.UtcNow;
            history.Success = true;
            history.PrintedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            entry.PrintStatus = PrintStatus.Failed;
            history.Success = false;
            history.ErrorMessage = ex.Message;
            history.PrintedAt = DateTime.UtcNow;
        }

        _db.PrintHistories.Add(history);
        _db.ChequeEntries.Update(entry);
        await _db.SaveChangesAsync();

        return history;
    }

    public async Task<List<PrintHistory>> PrintBatchAsync(List<ChequeEntry> entries, string? printerName = null)
    {
        var results = new List<PrintHistory>();
        foreach (var entry in entries)
        {
            var result = await PrintChequeAsync(entry, printerName);
            results.Add(result);
        }
        return results;
    }

    public async Task PrintAlignmentTestAsync(ChequeTemplate template, string? printerName = null)
    {
        var printDoc = new PrintDocument();
        if (!string.IsNullOrEmpty(printerName))
            printDoc.PrinterSettings.PrinterName = printerName;

        var paperSize = PaperSizeHelper.GetOrCreatePaperSize(
            printDoc.PrinterSettings, template.PaperWidthMm, template.PaperHeightMm);
        printDoc.DefaultPageSettings.PaperSize = paperSize;
        printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        printDoc.DefaultPageSettings.Landscape =
            template.PaperWidthMm > template.PaperHeightMm;

        printDoc.PrintPage += (sender, e) =>
        {
            if (e.Graphics != null)
                _engine.RenderAlignmentTest(e.Graphics, template);
            e.HasMorePages = false;
        };

        await Task.Run(() => printDoc.Print());
    }

    public List<string> GetAvailablePrinters()
    {
        var printers = new List<string>();
        foreach (string printer in PrinterSettings.InstalledPrinters)
            printers.Add(printer);
        return printers;
    }
}
