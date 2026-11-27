using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;

namespace ChequeMate.Infrastructure.Printing;

public static class UgandaChequeStandardValidator
{
    public const double ClearBandHeightMm = 16;
    public const double RestraintAreaWidthMm = 51;
    public const double WorkingLineRightMarginMm = 53;
    public const double HorizontalToleranceMm = 1.3;
    public const double VerticalToleranceMm = 1.6;

    private static readonly (double Width, double Height)[] StandardSizes =
    [
        (165, 73),
        (178, 76),
        (203, 101.6)
    ];

    public static string? ValidateProtectedZones(ChequeTemplate template)
    {
        if (!IsStandardPaperSize(template.PaperWidthMm, template.PaperHeightMm))
        {
            return $"The {template.PaperWidthMm:0.#} x {template.PaperHeightMm:0.#} mm paper size is not " +
                   "one of the BOU cheque families: 165 x 73, 178 x 76, or 203 x 101.6 mm.";
        }

        var clearBandTop = template.PaperHeightMm - ClearBandHeightMm;
        var restraintLeft = template.PaperWidthMm - RestraintAreaWidthMm;
        var workingLineRight = template.PaperWidthMm - WorkingLineRightMarginMm;

        foreach (var field in template.Fields)
        {
            var label = field.Label ?? field.FieldType.ToString();
            var left = field.PositionXMm + template.OffsetXMm;
            var top = field.PositionYMm + template.OffsetYMm;
            var right = left + field.WidthMm;
            var bottom = top + field.HeightMm;

            if (left < 0 || top < 0 || right > template.PaperWidthMm || bottom > template.PaperHeightMm)
                return $"Field '{label}' lies outside the cheque after printer offsets are applied.";

            if (bottom > clearBandTop)
                return $"Field '{label}' enters the protected 16 mm MICR clear band.";

            if (field.FieldType is FieldType.Payee or FieldType.AmountInWords && right > workingLineRight)
            {
                return $"Field '{label}' enters the protected right-hand restraint area. " +
                       "Payee and amount-in-words fields must end at least 53 mm from the right edge.";
            }

            if (field.FieldType is FieldType.AccountPayee or FieldType.Memo or FieldType.Custom &&
                right > restraintLeft)
            {
                return $"Field '{label}' enters the protected right-hand restraint area.";
            }

            if ((field.FieldType == FieldType.Date || field.FieldType == FieldType.AmountInFigures) &&
                left < restraintLeft)
            {
                return $"Field '{label}' must be wholly inside the controlled 51 mm right-hand restraint area.";
            }

            if (field.FieldType == FieldType.AmountInFigures)
            {
                var expectedLeft = template.PaperWidthMm - 46;
                var normalTop = template.PaperHeightMm - 44;
                var corporateTop = template.PaperHeightMm - 54;
                if (!Within(left, expectedLeft, HorizontalToleranceMm) ||
                    !Within(field.WidthMm, 41, HorizontalToleranceMm) ||
                    !Within(field.HeightMm, 9.5, VerticalToleranceMm) ||
                    (!Within(top, normalTop, VerticalToleranceMm) &&
                     !Within(top, corporateTop, VerticalToleranceMm)))
                {
                    return $"Field '{label}' does not match a BOU amount-box position.";
                }
            }

            if (field.FieldType == FieldType.Date)
            {
                var normalBaseline = template.PaperHeightMm - 54;
                var corporateBaseline = template.PaperHeightMm - 63.5;
                if (!Within(right, template.PaperWidthMm - 5, HorizontalToleranceMm) ||
                    (!Within(bottom, normalBaseline, VerticalToleranceMm) &&
                     !Within(bottom, corporateBaseline, VerticalToleranceMm)))
                {
                    return $"Field '{label}' does not match a BOU date-line position.";
                }
            }
        }

        return null;
    }

    public static bool IsStandardPaperSize(double widthMm, double heightMm) =>
        StandardSizes.Any(size =>
            Within(widthMm, size.Width, 0.05) &&
            Within(heightMm, size.Height, 0.05));

    private static bool Within(double actual, double expected, double tolerance) =>
        Math.Abs(actual - expected) <= tolerance;
}
