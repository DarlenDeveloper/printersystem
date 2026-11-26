using ChequeMate.Core.Enums;

namespace ChequeMate.Core.Models;

public class ChequeTemplate
{
    public int Id { get; set; }
    public int? BankProfileId { get; set; }
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Physical width of the cheque in millimeters.
    /// </summary>
    public double PaperWidthMm { get; set; }

    /// <summary>
    /// Physical height of the cheque in millimeters.
    /// </summary>
    public double PaperHeightMm { get; set; }

    /// <summary>
    /// Global horizontal offset in mm to fine-tune printer alignment.
    /// </summary>
    public double OffsetXMm { get; set; }

    /// <summary>
    /// Global vertical offset in mm to fine-tune printer alignment.
    /// </summary>
    public double OffsetYMm { get; set; }

    public TemplateVerificationStatus VerificationStatus { get; set; } =
        TemplateVerificationStatus.StandardTestOnly;

    public string VerificationLabel => VerificationStatus switch
    {
        TemplateVerificationStatus.Verified => "Verified for live use",
        TemplateVerificationStatus.LegacyCandidate => "Legacy candidate - test only",
        _ => "Uganda standard base - test only"
    };

    public string DisplayName => $"{TemplateName} [{VerificationLabel}]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public BankProfile? BankProfile { get; set; }
    public ICollection<TemplateField> Fields { get; set; } = new List<TemplateField>();
}
