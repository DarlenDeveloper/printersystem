using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;

namespace ChequeMate.Infrastructure.Printing;

public readonly record struct ChequePageLayout(
    double PageWidthMm,
    double PageHeightMm,
    double OriginXMm,
    double OriginYMm,
    bool RotateClockwise,
    double OccupiedWidthMm,
    double OccupiedHeightMm);

public static class ChequePageLayoutCalculator
{
    public const double A4WidthMm = 210;
    public const double A4HeightMm = 297;

    public static ChequePageLayout Calculate(ChequeTemplate template, PrintJobOptions options)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(options);

        var rotated = options.FeedOrientation == FeedOrientation.ShortEdgeFirst;
        var occupiedWidth = rotated ? template.PaperHeightMm : template.PaperWidthMm;
        var occupiedHeight = rotated ? template.PaperWidthMm : template.PaperHeightMm;
        var pageWidth = options.Mode == PrintMode.A4Positioned ? A4WidthMm : occupiedWidth;
        var pageHeight = options.Mode == PrintMode.A4Positioned ? A4HeightMm : occupiedHeight;

        if (occupiedWidth > pageWidth + 0.05 || occupiedHeight > pageHeight + 0.05)
        {
            throw new InvalidOperationException(
                $"The {template.PaperWidthMm:0.#} x {template.PaperHeightMm:0.#} mm cheque " +
                $"does not fit the selected {pageWidth:0.#} x {pageHeight:0.#} mm print page in this feed orientation.");
        }

        var originX = options.Mode == PrintMode.A4Positioned
            ? options.PaperAlignment switch
            {
                PaperAlignment.Left => 0,
                PaperAlignment.Centre => (pageWidth - occupiedWidth) / 2,
                _ => pageWidth - occupiedWidth
            }
            : 0;

        return new ChequePageLayout(
            pageWidth,
            pageHeight,
            originX + options.OffsetXMm,
            options.OffsetYMm,
            rotated,
            occupiedWidth,
            occupiedHeight);
    }
}
