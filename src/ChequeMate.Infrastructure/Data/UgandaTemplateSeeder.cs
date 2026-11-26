using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChequeMate.Infrastructure.Data;

public static class UgandaTemplateSeeder
{
    // Uganda Bankers' Association member banks and deposit-taking institutions
    // selected for the client bank picker. Non-bank development institutions and
    // MDIs that do not issue ordinary cheque books are intentionally excluded.
    private static readonly string[] SupportedBanks =
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

    public static void Seed(AppDbContext db)
    {
        var existingBanks = db.BankProfiles
            .Include(bank => bank.Templates)
            .ThenInclude(template => template.Fields)
            .ToList();

        foreach (var bankName in SupportedBanks)
        {
            var bank = existingBanks.FirstOrDefault(item =>
                string.Equals(item.BankName, bankName, StringComparison.OrdinalIgnoreCase));

            if (bank == null)
            {
                bank = new BankProfile
                {
                    BankName = bankName,
                    Country = "Uganda",
                    IsSupported = true
                };
                db.BankProfiles.Add(bank);
                existingBanks.Add(bank);
            }
            else
            {
                bank.Country = "Uganda";
                bank.IsSupported = true;
            }

            AddStandardTemplateIfMissing(bank, "Personal - 165 x 73 mm - TEST ONLY", 165, 73, false);
            AddStandardTemplateIfMissing(bank, "Corporate - 178 x 76 mm - TEST ONLY", 178, 76, true);
            AddStandardTemplateIfMissing(bank, "Corporate - 203 x 101.6 mm - TEST ONLY", 203, 101.6, true);
        }

        db.SaveChanges();
    }

    private static void AddStandardTemplateIfMissing(
        BankProfile bank,
        string name,
        double width,
        double height,
        bool corporate)
    {
        if (bank.Templates.Any(template =>
                string.Equals(template.TemplateName, name, StringComparison.OrdinalIgnoreCase)))
            return;

        var template = new ChequeTemplate
        {
            TemplateName = name,
            PaperWidthMm = width,
            PaperHeightMm = height,
            VerificationStatus = TemplateVerificationStatus.StandardTestOnly
        };

        var restraintLeft = width - 51;
        var unrestrainedRight = width - 53;
        var amountTop = corporate ? height - 54 : height - 44;
        var dateBaseline = corporate ? height - 63.5 : height - 54;

        template.Fields.Add(CreateField(
            FieldType.Payee, "Payee", 15, corporate ? 20 : 19,
            unrestrainedRight - 15, 8, 10));
        template.Fields.Add(CreateField(
            FieldType.AmountInWords, "Amount in words", 15, corporate ? 34 : 34,
            unrestrainedRight - 15, corporate && height > 80 ? 16 : 13, 9));
        template.Fields.Add(CreateField(
            FieldType.Date, "Date", restraintLeft + 3, Math.Max(2, dateBaseline - 8),
            43, 8, 10, TextAlignment.Right));
        template.Fields.Add(CreateField(
            FieldType.AmountInFigures, "Amount in figures", width - 46, amountTop,
            41, 9.5, 10, TextAlignment.Right, true));

        bank.Templates.Add(template);
    }

    private static TemplateField CreateField(
        FieldType type,
        string label,
        double x,
        double y,
        double width,
        double height,
        double fontSize,
        TextAlignment alignment = TextAlignment.Left,
        bool bold = false) => new()
        {
            FieldType = type,
            Label = label,
            PositionXMm = x,
            PositionYMm = y,
            WidthMm = width,
            HeightMm = height,
            FontFamily = "Arial",
            FontSizePt = fontSize,
            Alignment = alignment,
            IsBold = bold
        };
}
