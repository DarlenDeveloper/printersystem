using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using ChequeMate.Infrastructure.Data;
using ChequeMate.Infrastructure.Printing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChequeMate.Tests;

public class UgandaTemplateTests
{
    [Fact]
    public async Task Seeder_ExposesOnlySpecimenBackedBanksAndPresets()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        UgandaTemplateSeeder.Seed(db);

        Assert.Equal(29, await db.BankProfiles.CountAsync());
        Assert.Equal(5, await db.BankProfiles.CountAsync(bank => bank.IsSupported));
        Assert.Equal(8, await db.ChequeTemplates.CountAsync(template => template.IsEnabled));

        var templates = await db.ChequeTemplates
            .Include(template => template.Fields)
            .Where(template => template.IsEnabled)
            .ToListAsync();
        Assert.All(templates, template =>
        {
            Assert.Equal(TemplateVerificationStatus.SpecimenCandidate, template.VerificationStatus);
            Assert.False(string.IsNullOrWhiteSpace(template.CurrencyCode));
            Assert.False(string.IsNullOrWhiteSpace(template.SpecimenReference));
            Assert.Equal(4, template.Fields.Count);
            Assert.True(UgandaChequeStandardValidator.IsStandardPaperSize(
                template.PaperWidthMm,
                template.PaperHeightMm));
            Assert.Null(UgandaChequeStandardValidator.ValidateProtectedZones(template));
        });
    }

    [Fact]
    public async Task Seeder_IsIdempotentAndRetiresAnOlderGenericLayout()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        UgandaTemplateSeeder.Seed(db);
        var stanbic = await db.BankProfiles.SingleAsync(bank =>
            bank.BankName == "Stanbic Bank Uganda Limited");
        var generic = new ChequeTemplate
        {
            BankProfileId = stanbic.Id,
            TemplateName = "Corporate - 203 x 101.6 mm - TEST ONLY",
            IsEnabled = true,
            PaperWidthMm = 203,
            PaperHeightMm = 101.6,
            VerificationStatus = TemplateVerificationStatus.StandardTestOnly
        };
        db.ChequeTemplates.Add(generic);
        await db.SaveChangesAsync();

        UgandaTemplateSeeder.Seed(db);

        Assert.False((await db.ChequeTemplates.FindAsync(generic.Id))!.IsEnabled);
        Assert.Equal(8, await db.ChequeTemplates.CountAsync(template => template.IsEnabled));
        Assert.Equal(32, await db.TemplateFields.CountAsync(field => field.Template!.IsEnabled));
    }

    [Fact]
    public async Task Seeder_UsesFieldTestedSharedGeometryFor203MmLeavesOnly()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        UgandaTemplateSeeder.Seed(db);

        var sharedTemplates = await db.ChequeTemplates
            .Include(template => template.Fields)
            .Where(template => template.IsEnabled &&
                               template.PaperWidthMm == 203 &&
                               template.PaperHeightMm == 101.6)
            .ToListAsync();
        Assert.Equal(7, sharedTemplates.Count);

        foreach (var template in sharedTemplates)
        {
            var payee = template.Fields.Single(field => field.FieldType == FieldType.Payee);
            Assert.Equal((10d, 43d, 133d, 8d),
                (payee.PositionXMm, payee.PositionYMm, payee.WidthMm, payee.HeightMm));

            var words = template.Fields.Single(field => field.FieldType == FieldType.AmountInWords);
            Assert.Equal((30d, 56d, 113d, 19d),
                (words.PositionXMm, words.PositionYMm, words.WidthMm, words.HeightMm));
            Assert.Equal("2026-11-27-field-test-1", template.LayoutRevision);
        }

        var centenaryUsd = await db.ChequeTemplates
            .Include(template => template.Fields)
            .SingleAsync(template => template.IsEnabled &&
                                     template.CurrencyCode == "USD" &&
                                     template.BankProfile!.BankName ==
                                     "Centenary Rural Development Bank Limited");
        var centenaryPayee = centenaryUsd.Fields.Single(field => field.FieldType == FieldType.Payee);
        Assert.Equal((10d, 26d, 107d, 8d),
            (centenaryPayee.PositionXMm, centenaryPayee.PositionYMm,
             centenaryPayee.WidthMm, centenaryPayee.HeightMm));
    }

    [Fact]
    public void ProtectedZoneValidator_RejectsNonBouPaperSize()
    {
        var template = new ChequeTemplate
        {
            PaperWidthMm = 200,
            PaperHeightMm = 90
        };

        var error = UgandaChequeStandardValidator.ValidateProtectedZones(template);

        Assert.Contains("BOU", error);
    }

    [Fact]
    public void ProtectedZoneValidator_RejectsFieldInMicrClearBand()
    {
        var template = TemplateWithField(new TemplateField
        {
            FieldType = FieldType.Payee,
            PositionXMm = 10,
            PositionYMm = 60,
            WidthMm = 80,
            HeightMm = 8
        });

        var error = UgandaChequeStandardValidator.ValidateProtectedZones(template);

        Assert.Contains("MICR", error);
    }

    [Fact]
    public void ProtectedZoneValidator_RejectsPayeeInRightRestraintArea()
    {
        var template = TemplateWithField(new TemplateField
        {
            FieldType = FieldType.Payee,
            PositionXMm = 15,
            PositionYMm = 20,
            WidthMm = 120,
            HeightMm = 8
        });

        var error = UgandaChequeStandardValidator.ValidateProtectedZones(template);

        Assert.Contains("restraint", error);
    }

    private static ChequeTemplate TemplateWithField(TemplateField field)
    {
        var template = new ChequeTemplate
        {
            PaperWidthMm = 165,
            PaperHeightMm = 73
        };
        template.Fields.Add(field);
        return template;
    }
}
