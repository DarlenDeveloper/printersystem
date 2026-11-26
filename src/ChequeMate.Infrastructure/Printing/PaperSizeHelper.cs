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
    /// Finds a matching paper size already registered in the printer driver.
    /// Drivers commonly replace an unregistered in-memory custom size with A4,
    /// so printing is stopped when no matching driver size exists.
    /// </summary>
    public static PaperSize GetRequiredPaperSize(
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
        throw new InvalidOperationException(
            $"The '{printer.PrinterName}' driver has no registered {widthMm:0.#} x {heightMm:0.#} mm paper size. " +
            "Open Windows Printing Preferences for this printer, create a User-Defined size, save it, " +
            "then restart ChequeMate. The job was stopped to prevent the driver from silently using A4.");
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
