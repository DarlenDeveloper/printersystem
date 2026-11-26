using System.Drawing;
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

    public async Task<PrintHistory> PrintChequeAsync(
        ChequeEntry entry,
        string printerName,
        string paperSourceName)
    {
        var template = entry.Template
            ?? throw new InvalidOperationException("Cheque entry must have a loaded template.");

        if (entry.PrintStatus is PrintStatus.Printing or PrintStatus.Submitted or PrintStatus.Printed)
            throw new InvalidOperationException($"Cheque cannot be submitted while its status is {entry.PrintStatus}.");

        var persistedEntry = await _db.ChequeEntries.FindAsync(entry.Id)
            ?? throw new InvalidOperationException("The cheque entry no longer exists in the database.");

        var history = new PrintHistory
        {
            ChequeEntryId = entry.Id,
            PrinterName = printerName,
            PrintedAt = DateTime.UtcNow
        };
        var failureStage = "validating the cheque layout and printer media";

        try
        {
            ValidateTemplate(template);
            ValidateKnownPrinterMedia(printerName, template);
            failureStage = "recording the print attempt";
            entry.PrintStatus = PrintStatus.Printing;
            persistedEntry.PrintStatus = PrintStatus.Printing;
            await _db.SaveChangesAsync();

            failureStage = "configuring the Windows printer, paper source and exact paper size";
            using var printDocument = CreatePrintDocument(
                template,
                printerName,
                paperSourceName,
                useA4Overlay: false);

            printDocument.DocumentName = BuildDocumentName(entry);
            printDocument.PrintPage += (_, e) =>
            {
                if (e.Graphics != null)
                {
                    ConfigurePhysicalPageCoordinates(e);
                    _engine.RenderCheque(e.Graphics, entry, template);
                }

                e.HasMorePages = false;
            };

            // Print() returning means Windows accepted the job. It does not prove that
            // ink reached the cheque, so an operator must confirm it separately.
            failureStage = "submitting the job to the Windows print spooler";
            await Task.Run(printDocument.Print);

            entry.PrintStatus = PrintStatus.Submitted;
            history.Success = true;
        }
        catch (Exception ex)
        {
            entry.PrintStatus = PrintStatus.Failed;
            history.Success = false;
            history.ErrorMessage = BuildFailureReason(failureStage, ex);
        }

        _db.PrintHistories.Add(history);
        persistedEntry.PrintStatus = entry.PrintStatus;
        persistedEntry.PrintedAt = entry.PrintedAt;
        await _db.SaveChangesAsync();
        return history;
    }

    public async Task PrintAlignmentTestAsync(
        ChequeTemplate template,
        string printerName,
        string paperSourceName)
    {
        ValidateTemplate(template);
        var useA4Overlay = RequiresA4Overlay(printerName, template);
        if (!useA4Overlay)
            ValidateKnownPrinterMedia(printerName, template);

        using var printDocument = CreatePrintDocument(
            template,
            printerName,
            paperSourceName,
            useA4Overlay);

        printDocument.DocumentName = $"ChequeMate alignment - {template.TemplateName}";
        printDocument.PrintPage += (_, e) =>
        {
            if (e.Graphics != null)
            {
                ConfigurePhysicalPageCoordinates(e);
                _engine.RenderAlignmentTest(e.Graphics, template);
            }

            e.HasMorePages = false;
        };

        await Task.Run(printDocument.Print);
    }

    public List<string> GetAvailablePrinters()
    {
        var printers = new List<string>();
        foreach (string printer in PrinterSettings.InstalledPrinters)
            printers.Add(printer);
        return printers;
    }

    public List<string> GetAvailablePaperSources(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            return new List<string>();

        var settings = new PrinterSettings { PrinterName = printerName };
        if (!settings.IsValid)
            return new List<string>();

        var sources = new List<string>();
        foreach (PaperSource source in settings.PaperSources)
        {
            if (!string.IsNullOrWhiteSpace(source.SourceName))
                sources.Add(source.SourceName);
        }

        return sources.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static PrintDocument CreatePrintDocument(
        ChequeTemplate template,
        string printerName,
        string paperSourceName,
        bool useA4Overlay)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            throw new InvalidOperationException("Select a printer before printing.");
        if (string.IsNullOrWhiteSpace(paperSourceName))
            throw new InvalidOperationException("Select the rear/manual paper source before printing.");

        var document = new PrintDocument
        {
            OriginAtMargins = false,
            PrintController = new StandardPrintController()
        };

        document.PrinterSettings.PrinterName = printerName;
        if (!document.PrinterSettings.IsValid)
        {
            document.Dispose();
            throw new InvalidOperationException($"Printer '{printerName}' is not available.");
        }

        var source = document.PrinterSettings.PaperSources
            .Cast<PaperSource>()
            .FirstOrDefault(item => string.Equals(
                item.SourceName,
                paperSourceName,
                StringComparison.OrdinalIgnoreCase));

        if (source == null)
        {
            document.Dispose();
            throw new InvalidOperationException(
                $"Paper source '{paperSourceName}' is no longer available on '{printerName}'.");
        }

        var pageWidthMm = useA4Overlay ? 210 : template.PaperWidthMm;
        var pageHeightMm = useA4Overlay ? 297 : template.PaperHeightMm;
        var paperSize = PaperSizeHelper.GetRequiredPaperSize(
            document.PrinterSettings,
            pageWidthMm,
            pageHeightMm,
            out var landscape);

        document.DefaultPageSettings.PaperSource = source;
        document.DefaultPageSettings.PaperSize = paperSize;
        document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        document.DefaultPageSettings.Landscape = landscape;
        return document;
    }

    private static void ConfigurePhysicalPageCoordinates(PrintPageEventArgs e)
    {
        if (e.Graphics == null)
            return;

        e.Graphics.PageUnit = GraphicsUnit.Millimeter;

        // With OriginAtMargins=false, GDI starts at the printable area's top-left.
        // Move the origin back to the physical paper edge so template coordinates
        // remain measured from the cheque's top-left on every printer.
        var hardMarginXMm = e.PageSettings.HardMarginX * 25.4f / 100f;
        var hardMarginYMm = e.PageSettings.HardMarginY * 25.4f / 100f;
        e.Graphics.TranslateTransform(-hardMarginXMm, -hardMarginYMm);
    }

    private static void ValidateTemplate(ChequeTemplate template)
    {
        if (!double.IsFinite(template.PaperWidthMm) ||
            !double.IsFinite(template.PaperHeightMm) ||
            template.PaperWidthMm <= 0 ||
            template.PaperHeightMm <= 0)
        {
            throw new InvalidOperationException("Cheque paper dimensions must be positive numbers.");
        }

        if (template.Fields.Count == 0)
            throw new InvalidOperationException("The template has no printable fields.");

        foreach (var field in template.Fields)
        {
            var effectiveX = field.PositionXMm + template.OffsetXMm;
            var effectiveY = field.PositionYMm + template.OffsetYMm;
            if (effectiveX < 0 || effectiveY < 0 ||
                field.WidthMm <= 0 || field.HeightMm <= 0 ||
                effectiveX + field.WidthMm > template.PaperWidthMm ||
                effectiveY + field.HeightMm > template.PaperHeightMm)
            {
                throw new InvalidOperationException(
                    $"Field '{field.Label ?? field.FieldType.ToString()}' lies outside the cheque boundaries.");
            }
        }

        var protectedZoneError = UgandaChequeStandardValidator.ValidateProtectedZones(template);
        if (protectedZoneError != null)
            throw new InvalidOperationException(protectedZoneError);
    }

    private static void ValidateKnownPrinterMedia(string printerName, ChequeTemplate template)
    {
        if (RequiresA4Overlay(printerName, template))
        {
            throw new InvalidOperationException(
                "Epson L220 direct printing is not supported for this cheque size. " +
                "The L220's smallest supported sheet is 89 x 127 mm, while this cheque is " +
                $"{template.PaperWidthMm:0.#} x {template.PaperHeightMm:0.#} mm. " +
                "Use Alignment Test for an A4 overlay, or use a printer that officially supports the cheque leaf.");
        }

        var isCanonGx7000 = printerName.Contains("GX70", StringComparison.OrdinalIgnoreCase) ||
                            printerName.Contains("GX7000", StringComparison.OrdinalIgnoreCase);
        if (!isCanonGx7000)
            return;

        var shortEdge = Math.Min(template.PaperWidthMm, template.PaperHeightMm);
        var longEdge = Math.Max(template.PaperWidthMm, template.PaperHeightMm);
        if (shortEdge < 89 || shortEdge > 216 || longEdge < 127 || longEdge > 1200)
        {
            throw new InvalidOperationException(
                "Canon MAXIFY GX7000-series custom media must be 89-216 mm wide and 127-1200 mm long. " +
                $"This template is {template.PaperWidthMm:0.#} x {template.PaperHeightMm:0.#} mm.");
        }
    }

    private static bool RequiresA4Overlay(string printerName, ChequeTemplate template)
        => PaperSizeHelper.RequiresEpsonL220A4Overlay(
            printerName,
            template.PaperWidthMm,
            template.PaperHeightMm);

    private static string BuildDocumentName(ChequeEntry entry)
    {
        var identifier = string.IsNullOrWhiteSpace(entry.ChequeNumber)
            ? entry.Id.ToString()
            : entry.ChequeNumber;
        return $"ChequeMate cheque {identifier}";
    }

    private static string BuildFailureReason(string stage, Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message) &&
                !messages.Contains(current.Message, StringComparer.OrdinalIgnoreCase))
            {
                messages.Add(current.Message.Trim());
            }
        }

        var detail = messages.Count == 0
            ? exception.GetType().Name
            : string.Join(" -> ", messages);
        return $"Failed while {stage}: {detail} (Windows code 0x{exception.HResult:X8}).";
    }
}
