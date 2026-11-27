using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChequeMate.Infrastructure.Data;

public static class UgandaTemplateSeeder
{
    private const string LayoutRevision = "2026-11-27-field-test-1";

    // Confirmed by the 27 Nov 2026 Bank of India USD field test (DOC531.pdf).
    // The operator confirmed that the 203 x 101.6 mm leaves for BOI, DFCU,
    // Stanbic, Exim and Centenary UGX use the same writable field geometry.
    // Printer-specific movement (EPSON L220: X -10 mm, Y 0 mm in the test)
    // remains in PrinterProfileService and is shared by physical paper size.
    private static readonly FieldBox SharedCorporate203Payee = new(10, 43, 133, 8);
    private static readonly FieldBox SharedCorporate203AmountWords = new(30, 56, 113, 19);

    // Retained as the known institution catalogue. Only banks with specimen-backed
    // presets are marked supported and exposed in the client picker.
    private static readonly string[] KnownBanks =
    [
        "ABC Capital Bank Uganda",
        "Absa Bank Uganda Limited",
        "Bank of Africa Uganda Limited",
        "Bank of Baroda Uganda Limited",
        "Bank of India (Uganda) Limited",
        "BRAC Uganda Bank Limited",
        "Cairo Bank Uganda Limited",
        "Centenary Rural Development Bank Limited",
        "Citibank Uganda Limited",
        "DFCU Bank Limited",
        "Diamond Trust Bank Uganda Limited",
        "Ecobank Uganda Limited",
        "Equity Bank Uganda Limited",
        "Exim Bank Uganda Limited",
        "Finance Trust Bank Uganda Limited",
        "Guaranty Trust Bank Uganda Limited",
        "Housing Finance Bank Limited",
        "I&M Bank Uganda Limited",
        "KCB Bank Uganda Limited",
        "NCBA Bank Uganda Limited",
        "Opportunity Bank Uganda Limited",
        "Pearl Bank Uganda Limited",
        "Pride Bank Limited",
        "Salaam Bank Uganda Limited",
        "Stanbic Bank Uganda Limited",
        "Standard Chartered Bank Uganda Limited",
        "Tropical Bank Limited",
        "United Bank for Africa Uganda Limited",
        "Yako Bank Uganda Limited"
    ];

    private static readonly PresetDefinition[] Presets =
    [
        new(
            "Bank of India (Uganda) Limited",
            "Corporate 203 x 101.6 mm — UGX — specimen",
            "UGX", "Uganda Shillings", "Cents", "DOC517.pdf",
            203, 101.6, true,
            SharedCorporate203Payee,
            SharedCorporate203AmountWords),
        new(
            "Bank of India (Uganda) Limited",
            "Corporate 203 x 101.6 mm — USD — specimen",
            "USD", "US Dollars", "Cents", "DOC523.pdf; field test DOC531.pdf",
            203, 101.6, true,
            SharedCorporate203Payee,
            SharedCorporate203AmountWords),
        new(
            "Exim Bank Uganda Limited",
            "Corporate 203 x 101.6 mm — USD — specimen",
            "USD", "US Dollars", "Cents", "DOC518.pdf",
            203, 101.6, true,
            SharedCorporate203Payee,
            SharedCorporate203AmountWords),
        new(
            "Stanbic Bank Uganda Limited",
            "Corporate 203 x 101.6 mm — USD — specimen",
            "USD", "US Dollars", "Cents", "DOC519.pdf",
            203, 101.6, true,
            SharedCorporate203Payee,
            SharedCorporate203AmountWords),
        new(
            "Stanbic Bank Uganda Limited",
            "Corporate 203 x 101.6 mm — UGX — specimen",
            "UGX", "Uganda Shillings", "Cents", "DOC520.pdf",
            203, 101.6, true,
            SharedCorporate203Payee,
            SharedCorporate203AmountWords),
        new(
            "Centenary Rural Development Bank Limited",
            "Corporate 178 x 76 mm — USD — specimen",
            "USD", "US Dollars", "Cents", "DOC521.pdf",
            178, 76, false,
            new FieldBox(10, 26, 107, 8),
            new FieldBox(30, 37, 87, 17)),
        new(
            "Centenary Rural Development Bank Limited",
            "Corporate 203 x 101.6 mm — UGX — specimen",
            "UGX", "Uganda Shillings", "Cents", "DOC522.pdf",
            203, 101.6, true,
            SharedCorporate203Payee,
            SharedCorporate203AmountWords),
        new(
            "DFCU Bank Limited",
            "Corporate 203 x 101.6 mm — USD — specimen",
            "USD", "US Dollars", "Cents", "DOC530.pdf",
            203, 101.6, true,
            SharedCorporate203Payee,
            SharedCorporate203AmountWords)
    ];

    public static void Seed(AppDbContext db)
    {
        var banks = db.BankProfiles
            .Include(bank => bank.Templates)
            .ThenInclude(template => template.Fields)
            .ToList();

        foreach (var bank in banks)
            bank.IsSupported = false;

        foreach (var bankName in KnownBanks)
        {
            var bank = banks.FirstOrDefault(item =>
                string.Equals(item.BankName, bankName, StringComparison.OrdinalIgnoreCase));

            if (bank == null)
            {
                bank = new BankProfile { BankName = bankName };
                db.BankProfiles.Add(bank);
                banks.Add(bank);
            }

            bank.Country = "Uganda";
            bank.IsSupported = Presets.Any(preset =>
                string.Equals(preset.BankName, bankName, StringComparison.OrdinalIgnoreCase));

            // Generic layouts from earlier releases are retained for audit/history,
            // but they can no longer be selected or printed.
            foreach (var template in bank.Templates)
                template.IsEnabled = false;
        }

        foreach (var preset in Presets)
        {
            var bank = banks.Single(item =>
                string.Equals(item.BankName, preset.BankName, StringComparison.OrdinalIgnoreCase));
            UpsertPreset(db, bank, preset);
        }

        db.SaveChanges();
    }

    private static void UpsertPreset(
        AppDbContext db,
        BankProfile bank,
        PresetDefinition preset)
    {
        var template = bank.Templates.FirstOrDefault(item =>
            string.Equals(item.TemplateName, preset.TemplateName, StringComparison.OrdinalIgnoreCase));
        var isNew = template == null;
        template ??= new ChequeTemplate();

        template.TemplateName = preset.TemplateName;
        template.IsEnabled = true;
        template.CurrencyCode = preset.CurrencyCode;
        template.CurrencyName = preset.CurrencyName;
        template.FractionalUnitName = preset.FractionalUnitName;
        template.IncludeCurrencyInWords = false;
        template.SpecimenReference = preset.SpecimenReference;
        template.LayoutRevision = LayoutRevision;
        template.PaperWidthMm = preset.PaperWidthMm;
        template.PaperHeightMm = preset.PaperHeightMm;
        template.OffsetXMm = 0;
        template.OffsetYMm = 0;
        template.VerificationStatus = TemplateVerificationStatus.SpecimenCandidate;
        template.UpdatedAt = DateTime.UtcNow;

        if (!isNew)
        {
            db.TemplateFields.RemoveRange(template.Fields);
            template.Fields.Clear();
        }

        AddFields(template, preset);

        if (isNew)
            bank.Templates.Add(template);
    }

    private static void AddFields(ChequeTemplate template, PresetDefinition preset)
    {
        template.Fields.Add(CreateField(
            FieldType.Payee,
            "Payee",
            preset.Payee,
            10));
        template.Fields.Add(CreateField(
            FieldType.AmountInWords,
            "Amount in words",
            preset.AmountWords,
            9));

        var dateBaselineFromBottom = preset.UseCorporateAlternative ? 63.5 : 54;
        var date = new FieldBox(
            preset.PaperWidthMm - 48,
            preset.PaperHeightMm - dateBaselineFromBottom - 8,
            43,
            8);
        template.Fields.Add(CreateField(
            FieldType.Date,
            "Date",
            date,
            10,
            TextAlignment.Right));

        var amountUpperEdgeFromBottom = preset.UseCorporateAlternative ? 54 : 44;
        var amount = new FieldBox(
            preset.PaperWidthMm - 46,
            preset.PaperHeightMm - amountUpperEdgeFromBottom,
            41,
            9.5);
        template.Fields.Add(CreateField(
            FieldType.AmountInFigures,
            "Amount in figures",
            amount,
            10,
            TextAlignment.Left,
            true));
    }

    private static TemplateField CreateField(
        FieldType type,
        string label,
        FieldBox box,
        double fontSize,
        TextAlignment alignment = TextAlignment.Left,
        bool bold = false) => new()
        {
            FieldType = type,
            Label = label,
            PositionXMm = box.X,
            PositionYMm = box.Y,
            WidthMm = box.Width,
            HeightMm = box.Height,
            FontFamily = "Arial",
            FontSizePt = fontSize,
            Alignment = alignment,
            IsBold = bold
        };

    private sealed record PresetDefinition(
        string BankName,
        string TemplateName,
        string CurrencyCode,
        string CurrencyName,
        string FractionalUnitName,
        string SpecimenReference,
        double PaperWidthMm,
        double PaperHeightMm,
        bool UseCorporateAlternative,
        FieldBox Payee,
        FieldBox AmountWords);

    private readonly record struct FieldBox(double X, double Y, double Width, double Height);
}
