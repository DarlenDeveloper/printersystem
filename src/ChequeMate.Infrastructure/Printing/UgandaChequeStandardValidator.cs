using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;

namespace ChequeMate.Infrastructure.Printing;

public static class UgandaChequeStandardValidator
{
    public const double ClearBandHeightMm = 16;
    public const double RestraintAreaWidthMm = 51;
    public const double WorkingLineRightMarginMm = 53;

    public static string? ValidateProtectedZones(ChequeTemplate template)
    {
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
        }

        return null;
    }
}
