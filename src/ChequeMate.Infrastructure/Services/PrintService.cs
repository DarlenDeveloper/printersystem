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
        string? paperSourceName,
        PrintJobOptions options)
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
            ValidatePrintOptions(printerName, template, options);
            failureStage = "recording the print attempt";
            entry.PrintStatus = PrintStatus.Printing;
            persistedEntry.PrintStatus = PrintStatus.Printing;
            await _db.SaveChangesAsync();

            failureStage = "configuring the Windows printer and selected print method";
            using var printDocument = CreatePrintDocument(
                template,
                printerName,
                paperSourceName,
                options);

            printDocument.DocumentName = BuildDocumentName(entry);
            printDocument.PrintPage += (_, e) =>
            {
                if (e.Graphics != null)
                {
                    ConfigurePhysicalPageCoordinates(e);
                    ApplyChequePlacement(e.Graphics, template, options);
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
        string? paperSourceName,
        PrintJobOptions options)
    {
        ValidateTemplate(template);
        ValidatePrintOptions(printerName, template, options);

        using var printDocument = CreatePrintDocument(
            template,
            printerName,
            paperSourceName,
            options);

        printDocument.DocumentName = $"ChequeMate alignment - {template.TemplateName}";
        printDocument.PrintPage += (_, e) =>
        {
            if (e.Graphics != null)
            {
                ConfigurePhysicalPageCoordinates(e);
                ApplyChequePlacement(e.Graphics, template, options);
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
        string? paperSourceName,
        PrintJobOptions options)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            throw new InvalidOperationException("Select a printer before printing.");

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

        if (!string.IsNullOrWhiteSpace(paperSourceName))
        {
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
                    $"Paper source '{paperSourceName}' is no longer available on '{printerName}'. " +
                    "Select Default source or refresh the printer list.");
            }

            document.DefaultPageSettings.PaperSource = source;
        }

        var layout = ChequePageLayoutCalculator.Calculate(template, options);
        var paperSize = options.Mode == PrintMode.A4Positioned
            ? PaperSizeHelper.GetA4PaperSize(document.PrinterSettings)
            : PaperSizeHelper.CreateDirectPaperSize(layout.PageWidthMm, layout.PageHeightMm);

        document.DefaultPageSettings.PaperSize = paperSize;
        document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        // The exact-size PaperSize is already created in its feed orientation.
        // A4-positioned mode always uses portrait A4.
        document.DefaultPageSettings.Landscape = false;
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

    private static void ApplyChequePlacement(
        Graphics graphics,
        ChequeTemplate template,
        PrintJobOptions options)
    {
        var layout = ChequePageLayoutCalculator.Calculate(template, options);

        if (layout.RotateClockwise)
        {
            // Template coordinates remain measured from the cheque's printed top-left.
            // Translate by the unrotated height so the 90-degree result stays positive.
            graphics.TranslateTransform(
                (float)(layout.OriginXMm + template.PaperHeightMm),
                (float)layout.OriginYMm);
            graphics.RotateTransform(90);
        }
        else
        {
            graphics.TranslateTransform((float)layout.OriginXMm, (float)layout.OriginYMm);
        }
    }

    private static void ValidateTemplate(ChequeTemplate template)
    {
        if (!template.IsEnabled)
        {
            throw new InvalidOperationException(
                "This cheque layout has been retired because it was not backed by a reviewed specimen. " +
                "Create a new queue entry using one of the current bank, leaf and currency presets.");
        }

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

    private static void ValidatePrintOptions(
        string printerName,
        ChequeTemplate template,
        PrintJobOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!double.IsFinite(options.OffsetXMm) || !double.IsFinite(options.OffsetYMm) ||
            Math.Abs(options.OffsetXMm) > 25 || Math.Abs(options.OffsetYMm) > 25)
        {
            throw new InvalidOperationException(
                "Printer calibration offsets must be between -25 mm and 25 mm.");
        }

        if (options.Mode == PrintMode.ExactChequeSize &&
            PaperSizeHelper.IsKnownDirectFeedUnsupported(
                printerName,
                template.PaperWidthMm,
                template.PaperHeightMm))
        {
            throw new InvalidOperationException(
                $"'{printerName}' cannot officially feed a {template.PaperWidthMm:0.#} x " +
                $"{template.PaperHeightMm:0.#} mm sheet. Its minimum supported sheet is 89 x 127 mm. " +
                "Choose A4 positioned mode for a plain-A4 alignment test, or use supported cheque hardware.");
        }

        _ = ChequePageLayoutCalculator.Calculate(template, options);
    }

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
