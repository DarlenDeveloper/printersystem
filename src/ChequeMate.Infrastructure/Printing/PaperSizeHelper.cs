using System.Drawing.Printing;

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
    /// Finds a matching paper size from the printer's supported sizes,
    /// or returns a custom one if no match found.
    /// </summary>
    public static PaperSize GetOrCreatePaperSize(
        PrinterSettings printer,
        double widthMm,
        double heightMm,
        out bool landscape)
    {
        ValidateDimensions(widthMm, heightMm);
        var templateIsLandscape = widthMm > heightMm;
        int targetWidth = MmToHundredthsOfInch(Math.Min(widthMm, heightMm));
        int targetHeight = MmToHundredthsOfInch(Math.Max(widthMm, heightMm));

        // Check if printer already has a matching size (within 2mm tolerance)
        int tolerance = 8; // ~2mm in hundredths of inch
        foreach (PaperSize size in printer.PaperSizes)
        {
            var directMatch = Math.Abs(size.Width - targetWidth) <= tolerance &&
                              Math.Abs(size.Height - targetHeight) <= tolerance;
            var rotatedMatch = Math.Abs(size.Height - targetWidth) <= tolerance &&
                               Math.Abs(size.Width - targetHeight) <= tolerance;
            if (directMatch)
            {
                landscape = templateIsLandscape;
                return size;
            }
            if (rotatedMatch)
            {
                // Some drivers store user-defined forms in their already-rotated
                // orientation. Do not rotate such a form a second time.
                landscape = !templateIsLandscape;
                return size;
            }
        }

        landscape = templateIsLandscape;
        return CreateChequePaperSize(widthMm, heightMm);
    }

    public static int MmToHundredthsOfInch(double millimetres) =>
        (int)Math.Round(millimetres / 25.4 * 100, MidpointRounding.AwayFromZero);

    private static void ValidateDimensions(double widthMm, double heightMm)
    {
        if (!double.IsFinite(widthMm) || !double.IsFinite(heightMm) || widthMm <= 0 || heightMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthMm), "Paper dimensions must be positive finite values.");
    }
}
