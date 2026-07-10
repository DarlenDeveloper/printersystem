namespace ChequeMate.Core.Models;

public class ChequeTemplate
{
    public int Id { get; set; }
    public int? BankProfileId { get; set; }
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Path to the scanned cheque image used as the visual alignment guide.
    /// </summary>
    public string? ChequeImagePath { get; set; }

    /// <summary>
    /// Physical width of the cheque in millimeters.
    /// </summary>
    public double PaperWidthMm { get; set; }

    /// <summary>
    /// Physical height of the cheque in millimeters.
    /// </summary>
    public double PaperHeightMm { get; set; }

    /// <summary>
    /// DPI of the scanned image (used for pixel-to-mm mapping in the editor).
    /// </summary>
    public double? ImageDpi { get; set; }

    /// <summary>
    /// Global horizontal offset in mm to fine-tune printer alignment.
    /// </summary>
    public double OffsetXMm { get; set; }

    /// <summary>
    /// Global vertical offset in mm to fine-tune printer alignment.
    /// </summary>
    public double OffsetYMm { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public BankProfile? BankProfile { get; set; }
    public ICollection<TemplateField> Fields { get; set; } = new List<TemplateField>();
}
