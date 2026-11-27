using ChequeMate.Infrastructure.Printing;
using System.Globalization;

namespace ChequeMate.Tests;

public class ChequePrintEngineTests
{
    [Theory]
    [InlineData("5000", "5,000")]
    [InlineData("1000000", "1,000,000")]
    [InlineData("0.5", "0.50")]
    [InlineData("5000.25", "5,000.25")]
    public void FormatAmountFigures_OmitsZeroCentsAndAsterisks(
        string input,
        string expected)
    {
        var result = ChequePrintEngine.FormatAmountFigures(
            decimal.Parse(input, CultureInfo.InvariantCulture));

        Assert.Equal(expected, result);
        Assert.DoesNotContain("*", result);
        Assert.False(result.EndsWith(".00", StringComparison.Ordinal));
    }
}
