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
        // Convert mm to hundredths of an inch (1 inch = 25.4mm)
        int widthHundredths = (int)Math.Round(widthMm / 25.4 * 100);
        int heightHundredths = (int)Math.Round(heightMm / 25.4 * 100);

        return new PaperSize(name, widthHundredths, heightHundredths);
    }

    /// <summary>
    /// Finds a matching paper size from the printer's supported sizes,
    /// or returns a custom one if no match found.
    /// </summary>
    public static PaperSize GetOrCreatePaperSize(PrinterSettings printer, double widthMm, double heightMm)
    {
        int targetWidth = (int)Math.Round(widthMm / 25.4 * 100);
        int targetHeight = (int)Math.Round(heightMm / 25.4 * 100);

        // Check if printer already has a matching size (within 2mm tolerance)
        int tolerance = 8; // ~2mm in hundredths of inch
        foreach (PaperSize size in printer.PaperSizes)
        {
            if (Math.Abs(size.Width - targetWidth) <= tolerance &&
                Math.Abs(size.Height - targetHeight) <= tolerance)
            {
                return size;
            }
        }

        return CreateChequePaperSize(widthMm, heightMm);
    }
}
