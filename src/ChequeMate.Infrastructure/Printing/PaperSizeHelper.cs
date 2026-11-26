using System.Drawing.Printing;
using ChequeMate.Core.Enums;

namespace ChequeMate.Infrastructure.Printing;

/// <summary>
/// Helps create and find custom paper sizes for cheque printing.
/// </summary>
public static class PaperSizeHelper
{
    /// <summary>
    /// Creates a PaperSize matching the cheque template dimensions.
    /// Paper size uses hundredths of an inch.
    /// </summary>
    public static PaperSize CreateChequePaperSize(double widthMm, double heightMm, string name = "Cheque")
    {
        ValidateDimensions(widthMm, heightMm);

        // Windows printer drivers describe the base paper in portrait orientation.
        // PageSettings.Landscape rotates it for a cheque whose long edge is horizontal.
        var portraitWidthMm = Math.Min(widthMm, heightMm);
        var portraitHeightMm = Math.Max(widthMm, heightMm);

        // Convert mm to hundredths of an inch (1 inch = 25.4mm)
        int widthHundredths = MmToHundredthsOfInch(portraitWidthMm);
        int heightHundredths = MmToHundredthsOfInch(portraitHeightMm);

        return new PaperSize(name, widthHundredths, heightHundredths);
    }

    /// <summary>
    /// Creates the page requested by ChequeMate without requiring a named Windows form.
    /// The printer driver may still reject a size outside its supported physical range.
    /// </summary>
    public static PaperSize CreateDirectPaperSize(double pageWidthMm, double pageHeightMm) =>
        new(
            "ChequeMate exact size",
            MmToHundredthsOfInch(pageWidthMm),
            MmToHundredthsOfInch(pageHeightMm));

    public static PaperSize GetA4PaperSize(PrinterSettings printer)
    {
        foreach (PaperSize size in printer.PaperSizes)
        {
            if (size.Kind == PaperKind.A4 ||
                (Math.Abs(size.Width - 827) <= 8 && Math.Abs(size.Height - 1169) <= 8) ||
                (Math.Abs(size.Height - 827) <= 8 && Math.Abs(size.Width - 1169) <= 8))
            {
                return size;
            }
        }

        // A4 is a standard Windows DMPAPER value; this fallback avoids any custom form.
        return new PaperSize("A4", 827, 1169) { RawKind = (int)PaperKind.A4 };
    }

    public static bool IsKnownDirectFeedUnsupported(
        string printerName,
        double widthMm,
        double heightMm)
    {
        ValidateDimensions(widthMm, heightMm);
        var shortEdge = Math.Min(widthMm, heightMm);
        var longEdge = Math.Max(widthMm, heightMm);

        var isEpsonL220 = printerName.Contains("L220", StringComparison.OrdinalIgnoreCase);
        var isCanonGx7000 = printerName.Contains("GX70", StringComparison.OrdinalIgnoreCase) ||
                            printerName.Contains("GX7000", StringComparison.OrdinalIgnoreCase);

        return (isEpsonL220 || isCanonGx7000) && (shortEdge < 89 || longEdge < 127);
    }

    public static string DescribeMediaSupport(
        string printerName,
        double widthMm,
        double heightMm,
        PrintMode mode)
    {
        if (mode == PrintMode.A4Positioned)
        {
            return IsKnownDirectFeedUnsupported(printerName, widthMm, heightMm)
                ? "A4 test only: this printer cannot officially feed the cheque leaf itself because one edge is below 89 mm."
                : "No Windows custom form is needed. Use a full A4 test sheet first; after alignment, the cut sheet may be fed in the saved position.";
        }

        return IsKnownDirectFeedUnsupported(printerName, widthMm, heightMm)
            ? "Exact-size direct feed is blocked: this printer's minimum supported sheet is 89 x 127 mm."
            : "ChequeMate will send the exact dimensions directly. The driver may still reject or substitute the size; run Alignment Test first.";
    }

    public static int MmToHundredthsOfInch(double millimetres) =>
        (int)Math.Round(millimetres / 25.4 * 100, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Returns true when the named printer is an Epson L220 and the requested
    /// sheet is smaller than Epson's supported 89 x 127 mm single-sheet limit.
    /// </summary>
    public static bool RequiresEpsonL220A4Overlay(
        string printerName,
        double widthMm,
        double heightMm)
    {
        ValidateDimensions(widthMm, heightMm);
        if (!printerName.Contains("L220", StringComparison.OrdinalIgnoreCase))
            return false;

        var shortEdge = Math.Min(widthMm, heightMm);
        var longEdge = Math.Max(widthMm, heightMm);
        return shortEdge < 89 || longEdge < 127;
    }

    private static void ValidateDimensions(double widthMm, double heightMm)
    {
        if (!double.IsFinite(widthMm) || !double.IsFinite(heightMm) || widthMm <= 0 || heightMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthMm), "Paper dimensions must be positive finite values.");
    }
}
