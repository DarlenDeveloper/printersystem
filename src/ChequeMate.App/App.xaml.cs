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
    // Bump this number whenever you change the DB schema (add/remove columns, change types).
    // The app will automatically wipe and recreate the DB on next launch.
    private const int SchemaVersion = 2;

    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        EnsureDatabase();
    }

    private static void EnsureDatabase()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChequeMate", "chequemate.db");

        // If the DB exists, check if it was built with the current schema version.
        // If not, delete it so EnsureCreated rebuilds it cleanly.
        if (File.Exists(dbPath))
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                // Read stored schema version (table may not exist on first run)
                int storedVersion = 0;
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Value FROM AppMeta WHERE Key = 'SchemaVersion'";
                    var result = cmd.ExecuteScalar();
                    if (result != null) storedVersion = Convert.ToInt32(result);
                }
                catch { /* table doesn't exist yet */ }

                if (storedVersion != SchemaVersion)
                {
                    conn.Close();
                    SqliteConnection.ClearAllPools();
                    File.Delete(dbPath);
                }
            }
            catch
            {
                // Corrupted DB — delete and recreate
                SqliteConnection.ClearAllPools();
                File.Delete(dbPath);
            }
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        // Store the current schema version
        try
        {
            db.Database.ExecuteSqlRaw(
                "CREATE TABLE IF NOT EXISTS AppMeta (Key TEXT PRIMARY KEY, Value TEXT NOT NULL)");
            db.Database.ExecuteSqlRaw(
                $"INSERT OR REPLACE INTO AppMeta (Key, Value) VALUES ('SchemaVersion', '{SchemaVersion}')");
        }
        catch { /* non-critical */ }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChequeMate", "chequemate.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Services
        services.AddTransient<IAmountToWordsService, AmountToWordsService>();
        services.AddTransient<ITemplateService, TemplateService>();
        services.AddTransient<IChequeService, ChequeService>();
        services.AddTransient<IBankProfileService, BankProfileService>();
        services.AddTransient<IPrintService, PrintService>();
        services.AddSingleton<ChequePrintEngine>();

        // ViewModels
        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.TemplateEditorViewModel>();
        services.AddTransient<ViewModels.ChequeEntryViewModel>();
        services.AddTransient<ViewModels.PrintQueueViewModel>();
    }
}

