using ChequeMate.Core.Enums;

namespace ChequeMate.Core.Models;

public class ChequeTemplate
{
    public int Id { get; set; }
    public int? BankProfileId { get; set; }
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Disabled templates remain readable for historical queue entries but cannot
    /// be selected for a new cheque or submitted to a printer.
    /// </summary>
    public bool IsEnabled { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string FractionalUnitName { get; set; } = "Cents";

    /// <summary>
    /// False when the physical cheque already prints the currency wording before
    /// the amount-in-words line.
    /// </summary>
    public bool IncludeCurrencyInWords { get; set; }

    public string? SpecimenReference { get; set; }
    public string LayoutRevision { get; set; } = string.Empty;

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
        TemplateVerificationStatus.SpecimenCandidate => "Specimen-derived - test only",
        TemplateVerificationStatus.LegacyCandidate => "Legacy candidate - test only",
        _ => "Uganda standard base - test only"
    };

    public string DisplayName => $"{TemplateName} [{VerificationLabel}]";

    public string CurrencyDescription => string.IsNullOrWhiteSpace(CurrencyCode)
        ? CurrencyName
        : $"{CurrencyCode} — {CurrencyName}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public BankProfile? BankProfile { get; set; }
    public ICollection<TemplateField> Fields { get; set; } = new List<TemplateField>();
}
