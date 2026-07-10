using System.IO;
using System.Windows;
using ChequeMate.Core.Services;
using ChequeMate.Infrastructure.Data;
using ChequeMate.Infrastructure.Printing;
using ChequeMate.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChequeMate.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Ensure DB is created
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
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
