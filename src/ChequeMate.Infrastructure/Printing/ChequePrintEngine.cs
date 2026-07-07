using System.Drawing;
using System.Drawing.Printing;
using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;

namespace ChequeMate.Infrastructure.Printing;

/// <summary>
/// Core engine that renders cheque field text at precise coordinates on the physical cheque.
/// All positions are in mm, converted to printer device units at print time.
/// </summary>
public class ChequePrintEngine
{
    /// <summary>
    /// Renders a cheque entry onto a PrintDocument using the template field positions.
    /// </summary>
    public void RenderCheque(Graphics graphics, ChequeEntry entry, ChequeTemplate template)
    {
        // Printer graphics DPI
        float dpiX = graphics.DpiX;
        float dpiY = graphics.DpiY;

        foreach (var field in template.Fields)
        {
            var text = GetFieldText(field, entry);
            if (string.IsNullOrEmpty(text)) continue;

            // Convert mm to device units (inches * DPI)
            // 1 inch = 25.4 mm
            float x = (float)((field.PositionXMm + template.OffsetXMm) / 25.4 * dpiX);
            float y = (float)((field.PositionYMm + template.OffsetYMm) / 25.4 * dpiY);
            float width = (float)(field.WidthMm / 25.4 * dpiX);
            float height = (float)(field.HeightMm / 25.4 * dpiY);

            var fontStyle = field.IsBold ? FontStyle.Bold : FontStyle.Regular;
            using var font = new Font(field.FontFamily, (float)field.FontSizePt, fontStyle);
            using var brush = new SolidBrush(Color.Black);

            var format = new StringFormat();
            format.Alignment = field.Alignment switch
            {
                Core.Enums.TextAlignment.Center => StringAlignment.Center,
                Core.Enums.TextAlignment.Right => StringAlignment.Far,
                _ => StringAlignment.Near
            };
            format.LineAlignment = StringAlignment.Center;
            format.Trimming = StringTrimming.EllipsisCharacter;

            var rect = new RectangleF(x, y, width, height);
            graphics.DrawString(text, font, brush, rect, format);
        }
    }

    /// <summary>
    /// Renders an alignment test grid for calibration.
    /// Prints crosshairs and field outlines so user can overlay on a real cheque.
    /// </summary>
    public void RenderAlignmentTest(Graphics graphics, ChequeTemplate template)
    {
        float dpiX = graphics.DpiX;
        float dpiY = graphics.DpiY;

        using var pen = new Pen(Color.Black, 0.5f);
        using var font = new Font("Arial", 6f);
        using var brush = new SolidBrush(Color.Black);

        // Draw border (cheque outline)
        float totalWidth = (float)(template.PaperWidthMm / 25.4 * dpiX);
        float totalHeight = (float)(template.PaperHeightMm / 25.4 * dpiY);
        graphics.DrawRectangle(pen, 0, 0, totalWidth - 1, totalHeight - 1);

        // Draw 10mm grid
        for (double mmX = 10; mmX < template.PaperWidthMm; mmX += 10)
        {
            float x = (float)(mmX / 25.4 * dpiX);
            graphics.DrawLine(pen, x, 0, x, totalHeight);
        }
        for (double mmY = 10; mmY < template.PaperHeightMm; mmY += 10)
        {
            float y = (float)(mmY / 25.4 * dpiY);
            graphics.DrawLine(pen, 0, y, totalWidth, y);
        }

        // Draw field outlines with labels
        using var fieldPen = new Pen(Color.Red, 1f);
        foreach (var field in template.Fields)
        {
            float x = (float)((field.PositionXMm + template.OffsetXMm) / 25.4 * dpiX);
            float y = (float)((field.PositionYMm + template.OffsetYMm) / 25.4 * dpiY);
            float w = (float)(field.WidthMm / 25.4 * dpiX);
            float h = (float)(field.HeightMm / 25.4 * dpiY);

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
            FieldType.AmountInFigures => entry.AmountFigures.ToString("N2"),
            FieldType.AccountPayee => "A/C Payee Only",
            FieldType.ChequeNumber => entry.ChequeNumber ?? "",
            FieldType.Memo => entry.Memo ?? "",
            FieldType.Custom => field.Label ?? "",
            _ => ""
        };
    }
}
