using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using ChequeMate.Infrastructure.Services;

namespace ChequeMate.Tests;

public class PrinterProfileServiceTests
{
    [Fact]
    public void SaveLoadAndClear_RoundTripsPerPrinterAndChequeSize()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"chequemate-printer-profiles-{Guid.NewGuid():N}.json");

        try
        {
            var service = new PrinterProfileService(path);
            service.Save(new PrinterProfile
            {
                PrinterName = "EPSON L220 Series",
                ChequeWidthMm = 203,
                ChequeHeightMm = 101.6,
                PaperSourceName = "Rear Feed",
                Options = new PrintJobOptions
                {
                    Mode = PrintMode.A4Positioned,
                    FeedOrientation = FeedOrientation.LongEdgeFirst,
                    PaperAlignment = PaperAlignment.Right,
                    OffsetXMm = 1.25,
                    OffsetYMm = -0.5
                }
            });

            var saved = service.GetOrCreate("epson l220 series", 203, 101.6);
            Assert.Equal("Rear Feed", saved.PaperSourceName);
            Assert.Equal(1.25, saved.Options.OffsetXMm);
            Assert.Equal(-0.5, saved.Options.OffsetYMm);
            Assert.NotEqual(default, saved.UpdatedAt);

            service.Clear("EPSON L220 Series", 203, 101.6);
            var cleared = service.GetOrCreate("EPSON L220 Series", 203, 101.6);
            Assert.Null(cleared.PaperSourceName);
            Assert.Equal(0, cleared.Options.OffsetXMm);
            Assert.Equal(default, cleared.UpdatedAt);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(path + ".tmp"))
                File.Delete(path + ".tmp");
        }
    }
}
