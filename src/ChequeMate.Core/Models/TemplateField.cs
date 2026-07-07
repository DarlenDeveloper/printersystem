using ChequeMate.Core.Enums;

namespace ChequeMate.Core.Models;

public class TemplateField
{
    public int Id { get; set; }
    public int TemplateId { get; set; }

    public FieldType FieldType { get; set; }
    public string? Label { get; set; }

    /// <summary>
    /// X position from left edge of cheque in mm.
    /// </summary>
    public double PositionXMm { get; set; }

    /// <summary>
    /// Y position from top edge of cheque in mm.
    /// </summary>
    public double PositionYMm { get; set; }

    /// <summary>
    /// Field width in mm.
    /// </summary>
    public double WidthMm { get; set; }

    /// <summary>
    /// Field height in mm.
    /// </summary>
    public double HeightMm { get; set; }

    public string FontFamily { get; set; } = "Arial";
    public double FontSizePt { get; set; } = 10;
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public bool IsBold { get; set; }

    public ChequeTemplate? Template { get; set; }
}
