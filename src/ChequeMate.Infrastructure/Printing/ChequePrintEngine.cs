using System.Drawing;
using System.Globalization;
using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;

namespace ChequeMate.Infrastructure.Printing;

/// <summary>
/// Core engine that renders cheque field text at precise coordinates on the physical cheque.
/// All positions are in millimetres. The printer Graphics object is explicitly
/// configured to use millimetres so output is independent of printer DPI.
/// </summary>
public class ChequePrintEngine
{
    /// <summary>
    /// Renders a cheque entry onto a PrintDocument using the template field positions.
    /// </summary>
    public void RenderCheque(Graphics graphics, ChequeEntry entry, ChequeTemplate template)
    {
        graphics.PageUnit = GraphicsUnit.Millimeter;

        foreach (var field in template.Fields)
        {
            var text = GetFieldText(field, entry);
            if (string.IsNullOrEmpty(text)) continue;

            float x = (float)(field.PositionXMm + template.OffsetXMm);
            float y = (float)(field.PositionYMm + template.OffsetYMm);
            float width = (float)field.WidthMm;
            float height = (float)field.HeightMm;

            using var brush = new SolidBrush(Color.Black);

            using var format = new StringFormat();
            format.Alignment = field.Alignment switch
            {
                Core.Enums.TextAlignment.Center => StringAlignment.Center,
                Core.Enums.TextAlignment.Right => StringAlignment.Far,
                _ => StringAlignment.Near
            };
            format.LineAlignment = StringAlignment.Center;
            format.Trimming = StringTrimming.None;
            format.FormatFlags = StringFormatFlags.LineLimit;
            if (field.FieldType != FieldType.AmountInWords)
                format.FormatFlags |= StringFormatFlags.NoWrap;

            var rect = new RectangleF(x, y, width, height);
            DrawFittedText(graphics, text, field, rect, format, brush);
        }
    }

    /// <summary>
    /// Renders an alignment test grid for calibration.
    /// Prints crosshairs and field outlines so user can overlay on a real cheque.
    /// </summary>
    public void RenderAlignmentTest(Graphics graphics, ChequeTemplate template)
    {
        graphics.PageUnit = GraphicsUnit.Millimeter;

        using var pen = new Pen(Color.Black, 0.2f);
        using var font = new Font("Arial", 6f);
        using var brush = new SolidBrush(Color.Black);

        // Draw border (cheque outline)
        float totalWidth = (float)template.PaperWidthMm;
        float totalHeight = (float)template.PaperHeightMm;
        graphics.DrawRectangle(pen, 0, 0, totalWidth - 0.2f, totalHeight - 0.2f);

        // Draw 10mm grid
        for (double mmX = 10; mmX < template.PaperWidthMm; mmX += 10)
        {
            float x = (float)mmX;
            graphics.DrawLine(pen, x, 0, x, totalHeight);
        }
        for (double mmY = 10; mmY < template.PaperHeightMm; mmY += 10)
        {
            float y = (float)mmY;
            graphics.DrawLine(pen, 0, y, totalWidth, y);
        }

        // Draw field outlines with labels
        var clearBandTop = totalHeight - (float)UgandaChequeStandardValidator.ClearBandHeightMm;
        var restraintLeft = totalWidth - (float)UgandaChequeStandardValidator.RestraintAreaWidthMm;
        using var protectedBrush = new SolidBrush(Color.FromArgb(45, Color.OrangeRed));
        using var protectedPen = new Pen(Color.OrangeRed, 0.4f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        graphics.FillRectangle(protectedBrush, 0, clearBandTop, totalWidth, (float)UgandaChequeStandardValidator.ClearBandHeightMm);
        graphics.DrawRectangle(protectedPen, 0, clearBandTop, totalWidth, (float)UgandaChequeStandardValidator.ClearBandHeightMm);
        graphics.DrawString("PROTECTED MICR CLEAR BAND - DO NOT PRINT", font, brush, 3, clearBandTop + 3);
        graphics.FillRectangle(protectedBrush, restraintLeft, 0, (float)UgandaChequeStandardValidator.RestraintAreaWidthMm, clearBandTop);
        graphics.DrawRectangle(protectedPen, restraintLeft, 0, (float)UgandaChequeStandardValidator.RestraintAreaWidthMm, clearBandTop);
        graphics.DrawString("CONTROLLED 51 mm RESTRAINT AREA", font, brush, restraintLeft + 2, 2);

        // Draw field outlines with labels
        using var fieldPen = new Pen(Color.Red, 0.3f);
        foreach (var field in template.Fields)
        {
            float x = (float)(field.PositionXMm + template.OffsetXMm);
            float y = (float)(field.PositionYMm + template.OffsetYMm);
            float w = (float)field.WidthMm;
            float h = (float)field.HeightMm;

            graphics.DrawRectangle(fieldPen, x, y, w, h);
            var label = field.Label ?? field.FieldType.ToString();
            graphics.DrawString(label, font, brush, x + 2, y + 2);
        }
    }

    private static string GetFieldText(TemplateField field, ChequeEntry entry)
    {
        return field.FieldType switch
        {
            FieldType.Payee => entry.Payee,
            FieldType.Date => entry.ChequeDate.ToString("dd/MM/yyyy"),
            FieldType.AmountInWords => entry.AmountWords,
            FieldType.AmountInFigures =>
                $"**{entry.AmountFigures.ToString("#,##0.00", CultureInfo.InvariantCulture)}**",
            FieldType.AccountPayee => "A/C Payee Only",
            FieldType.ChequeNumber => entry.ChequeNumber ?? "",
            FieldType.Memo => entry.Memo ?? "",
            FieldType.Custom => field.Label ?? "",
            _ => ""
        };
    }

    private static void DrawFittedText(
        Graphics graphics,
        string text,
        TemplateField field,
        RectangleF rect,
        StringFormat format,
        Brush brush)
    {
        var fontStyle = field.IsBold ? FontStyle.Bold : FontStyle.Regular;
        var minimumSize = field.FieldType switch
        {
            FieldType.AmountInWords => 7f,
            FieldType.Payee => 7f,
            FieldType.AmountInFigures => 7.5f,
            _ => 8f
        };

        for (var size = (float)field.FontSizePt; size >= minimumSize; size -= 0.5f)
        {
            using var font = new Font(field.FontFamily, size, fontStyle);
            graphics.MeasureString(
                text,
                font,
                new SizeF(rect.Width, rect.Height),
                format,
                out var charactersFitted,
                out _);

            if (charactersFitted < text.Length)
                continue;

            graphics.DrawString(text, font, brush, rect, format);
            return;
        }

        throw new InvalidOperationException(
            $"The {field.Label ?? field.FieldType.ToString()} text does not fit its approved cheque area. " +
            "Shorten the payee or reduce the amount wording before printing.");
    }
}
