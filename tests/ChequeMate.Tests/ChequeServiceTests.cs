using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using ChequeMate.Infrastructure.Data;
using ChequeMate.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChequeMate.Tests;

public class ChequeServiceTests
{
    [Fact]
    public async Task ConfirmPrinted_RequiresSubmittedStatus()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var entry = await fixture.AddChequeAsync(PrintStatus.Pending);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.ConfirmPrintedAsync(entry.Id));
    }

    [Fact]
    public async Task ConfirmPrinted_MarksSubmittedChequePrinted()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var entry = await fixture.AddChequeAsync(PrintStatus.Submitted);

        await fixture.Service.ConfirmPrintedAsync(entry.Id);

        var saved = await fixture.Context.ChequeEntries.FindAsync(entry.Id);
        Assert.Equal(PrintStatus.Printed, saved!.PrintStatus);
        Assert.NotNull(saved.PrintedAt);
    }

    [Fact]
    public async Task Queue_IncludesSubmittedAndExcludesConfirmedPrinted()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var submitted = await fixture.AddChequeAsync(PrintStatus.Submitted, "Submitted");
        await fixture.AddChequeAsync(PrintStatus.Printed, "Printed");

        var queue = await fixture.Service.GetPendingEntriesAsync();

        Assert.Single(queue);
        Assert.Equal(submitted.Id, queue[0].Id);
    }

    [Fact]
    public async Task CsvImport_HandlesQuotedCommas()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var filePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(
                filePath,
                "Payee,Amount,Date,ChequeNumber,Memo\n\"Acme, Ltd\",1250.50,2026-11-04,000123,\"Invoice, November\"");

            var entries = await fixture.Service.ImportFromCsvAsync(
                filePath,
                fixture.Template.Id,
                "Uganda Shillings",
                "Cents");

            Assert.Single(entries);
            Assert.Equal("Acme, Ltd", entries[0].Payee);
            Assert.Equal("Invoice, November", entries[0].Memo);
            Assert.StartsWith("Uganda Shillings", entries[0].AmountWords);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private sealed class DatabaseFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private DatabaseFixture(
            SqliteConnection connection,
            AppDbContext context,
            ChequeService service,
            ChequeTemplate template)
        {
            _connection = connection;
            Context = context;
            Service = service;
            Template = template;
        }

        public AppDbContext Context { get; }
        public ChequeService Service { get; }
        public ChequeTemplate Template { get; }

        public static async Task<DatabaseFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var template = new ChequeTemplate
            {
                TemplateName = "Test",
                PaperWidthMm = 200,
                PaperHeightMm = 90
            };
            context.ChequeTemplates.Add(template);
            await context.SaveChangesAsync();

            var service = new ChequeService(context, new AmountToWordsService());
            return new DatabaseFixture(connection, context, service, template);
        }

        public async Task<ChequeEntry> AddChequeAsync(
            PrintStatus status,
            string payee = "Test Payee")
        {
            var entry = new ChequeEntry
            {
                TemplateId = Template.Id,
                Payee = payee,
                AmountFigures = 100,
                AmountWords = "One Hundred Only",
                PrintStatus = status
            };
            Context.ChequeEntries.Add(entry);
            await Context.SaveChangesAsync();
            return entry;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
