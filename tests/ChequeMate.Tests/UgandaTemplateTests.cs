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
    public async Task Seeder_CreatesSupportedBanksAndThreeTestLayoutsPerBank()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        UgandaTemplateSeeder.Seed(db);

        Assert.Equal(29, await db.BankProfiles.CountAsync(bank => bank.IsSupported));
        Assert.Equal(87, await db.ChequeTemplates.CountAsync());
        Assert.All(
            await db.ChequeTemplates.ToListAsync(),
            template => Assert.Equal(TemplateVerificationStatus.StandardTestOnly, template.VerificationStatus));
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
