using System.IO;
using System.Windows;
using ChequeMate.Core.Services;
using ChequeMate.Infrastructure.Data;
using ChequeMate.Infrastructure.Printing;
using ChequeMate.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChequeMate.App;

public partial class App : Application
{
    private const int SchemaVersion = 6;

    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        try
        {
            EnsureDatabase();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"ChequeMate could not safely open its database. No data was deleted.\n\n{ex.Message}",
                "Database error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        base.OnStartup(e);
    }

    private static void EnsureDatabase()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChequeMate", "chequemate.db");

        if (File.Exists(dbPath))
        {
            ValidateDatabase(dbPath);
            var storedVersion = ReadSchemaVersion(dbPath);
            if (storedVersion != SchemaVersion)
                BackUpDatabase(dbPath, storedVersion);
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        EnsureSupportedBankColumn(dbPath);
        EnsureTemplateVerificationColumn(dbPath);
        EnsureTemplatePresetColumns(dbPath);
        UgandaTemplateSeeder.Seed(db);

        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS AppMeta (Key TEXT PRIMARY KEY, Value TEXT NOT NULL)");
        db.Database.ExecuteSqlInterpolated(
            $"INSERT OR REPLACE INTO AppMeta (Key, Value) VALUES ('SchemaVersion', {SchemaVersion})");
    }

    private static void EnsureSupportedBankColumn(string dbPath)
    {
        EnsureColumn(
            dbPath,
            "BankProfiles",
            "IsSupported",
            "ALTER TABLE BankProfiles ADD COLUMN IsSupported INTEGER NOT NULL DEFAULT 0");
    }

    private static void EnsureTemplateVerificationColumn(string dbPath)
    {
        EnsureColumn(
            dbPath,
            "ChequeTemplates",
            "VerificationStatus",
            "ALTER TABLE ChequeTemplates ADD COLUMN VerificationStatus TEXT NOT NULL DEFAULT 'StandardTestOnly'");
    }

    private static void EnsureTemplatePresetColumns(string dbPath)
    {
        EnsureColumn(
            dbPath,
            "ChequeTemplates",
            "IsEnabled",
            "ALTER TABLE ChequeTemplates ADD COLUMN IsEnabled INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(
            dbPath,
            "ChequeTemplates",
            "CurrencyCode",
            "ALTER TABLE ChequeTemplates ADD COLUMN CurrencyCode TEXT NOT NULL DEFAULT ''");
        EnsureColumn(
            dbPath,
            "ChequeTemplates",
            "CurrencyName",
            "ALTER TABLE ChequeTemplates ADD COLUMN CurrencyName TEXT NOT NULL DEFAULT ''");
        EnsureColumn(
            dbPath,
            "ChequeTemplates",
            "FractionalUnitName",
            "ALTER TABLE ChequeTemplates ADD COLUMN FractionalUnitName TEXT NOT NULL DEFAULT 'Cents'");
        EnsureColumn(
            dbPath,
            "ChequeTemplates",
            "IncludeCurrencyInWords",
            "ALTER TABLE ChequeTemplates ADD COLUMN IncludeCurrencyInWords INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(
            dbPath,
            "ChequeTemplates",
            "SpecimenReference",
            "ALTER TABLE ChequeTemplates ADD COLUMN SpecimenReference TEXT NULL");
        EnsureColumn(
            dbPath,
            "ChequeTemplates",
            "LayoutRevision",
            "ALTER TABLE ChequeTemplates ADD COLUMN LayoutRevision TEXT NOT NULL DEFAULT ''");
    }

    private static void EnsureColumn(
        string dbPath,
        string tableName,
        string columnName,
        string alterSql)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var inspect = connection.CreateCommand();
        inspect.CommandText = $"PRAGMA table_info({tableName})";
        using var reader = inspect.ExecuteReader();
        var hasColumn = false;
        while (reader.Read())
        {
            if (string.Equals(
                    reader.GetString(reader.GetOrdinal("name")),
                    columnName,
                    StringComparison.OrdinalIgnoreCase))
            {
                hasColumn = true;
                break;
            }
        }

        if (hasColumn)
            return;

        reader.Close();
        using var upgrade = connection.CreateCommand();
        upgrade.CommandText = alterSql;
        upgrade.ExecuteNonQuery();
    }

    private static void ValidateDatabase(string dbPath)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA quick_check";
            var result = command.ExecuteScalar()?.ToString();
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"SQLite integrity check returned: {result ?? "no result"}");
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException(
                $"The database at '{dbPath}' could not be validated. Restore it from a backup or contact support.",
                ex);
        }
    }

    private static int ReadSchemaVersion(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Value FROM AppMeta WHERE Key = 'SchemaVersion' AND " +
            "EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'AppMeta')";

        try
        {
            return command.ExecuteScalar() is { } value && int.TryParse(value.ToString(), out var version)
                ? version
                : 0;
        }
        catch (SqliteException)
        {
            return 0;
        }
    }

    private static void BackUpDatabase(string dbPath, int storedVersion)
    {
        var databaseDirectory = Path.GetDirectoryName(dbPath)!;
        var backupDirectory = Path.Combine(databaseDirectory, "Backups");
        Directory.CreateDirectory(backupDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupPath = Path.Combine(
            backupDirectory,
            $"chequemate-v{storedVersion}-{timestamp}.db");

        using var source = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        using var destination = new SqliteConnection($"Data Source={backupPath}");
        source.Open();
        destination.Open();
        source.BackupDatabase(destination);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChequeMate", "chequemate.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<AppDbContext>(
            options => options.UseSqlite($"Data Source={dbPath}"),
            contextLifetime: ServiceLifetime.Transient);

        // Services
        services.AddTransient<IAmountToWordsService, AmountToWordsService>();
        services.AddTransient<ITemplateService, TemplateService>();
        services.AddTransient<IChequeService, ChequeService>();
        services.AddTransient<IBankProfileService, BankProfileService>();
        services.AddTransient<IPrintService, PrintService>();
        services.AddSingleton<IPrinterProfileService, PrinterProfileService>();
        services.AddSingleton<ChequePrintEngine>();

        // ViewModels
        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.ChequeEntryViewModel>();
        services.AddTransient<ViewModels.PrintQueueViewModel>();
    }
}

