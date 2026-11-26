using ChequeMate.Core.Models;

namespace ChequeMate.Tests;

public class ChequeEntryTests
{
    [Fact]
    public void LastPrintError_ReturnsReasonFromLatestFailedAttempt()
    {
        var entry = new ChequeEntry
        {
            PrintHistories =
            {
                new PrintHistory
                {
                    Id = 1,
                    PrintedAt = new DateTime(2026, 11, 26, 8, 0, 0, DateTimeKind.Utc),
                    Success = false,
                    ErrorMessage = "Earlier failure"
                },
                new PrintHistory
                {
                    Id = 2,
                    PrintedAt = new DateTime(2026, 11, 26, 9, 0, 0, DateTimeKind.Utc),
                    Success = false,
                    ErrorMessage = "Exact paper size is not registered"
                }
            }
        };

        Assert.Equal("Exact paper size is not registered", entry.LastPrintError);
    }

    [Fact]
    public void LastPrintError_IsEmptyWhenLatestAttemptSucceeded()
    {
        var entry = new ChequeEntry
        {
            PrintHistories =
            {
                new PrintHistory
                {
                    Id = 1,
                    PrintedAt = new DateTime(2026, 11, 26, 8, 0, 0, DateTimeKind.Utc),
                    Success = false,
                    ErrorMessage = "Earlier failure"
                },
                new PrintHistory
                {
                    Id = 2,
                    PrintedAt = new DateTime(2026, 11, 26, 9, 0, 0, DateTimeKind.Utc),
                    Success = true
                }
            }
        };

        Assert.Null(entry.LastPrintError);
    }
}
