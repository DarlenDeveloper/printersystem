using ChequeMate.Infrastructure.Services;
using System.Globalization;

namespace ChequeMate.Tests;

public class AmountToWordsServiceTests
{
    private readonly AmountToWordsService _service = new();

    [Theory]
    [InlineData("0", "Uganda Shillings Zero Only")]
    [InlineData("1500.50", "Uganda Shillings One Thousand Five Hundred and Fifty Cents Only")]
    [InlineData("100000", "Uganda Shillings One Hundred Thousand Only")]
    [InlineData("1000000", "Uganda Shillings One Million Only")]
    [InlineData("1000001", "Uganda Shillings One Million and One Only")]
    public void Convert_UsesInternationalNumbering(string input, string expected)
    {
        var result = _service.Convert(
            decimal.Parse(input, CultureInfo.InvariantCulture),
            "Uganda Shillings",
            "Cents");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_RoundsFractionAwayFromZero()
    {
        var result = _service.Convert(1.005m, "Shillings", "Cents");

        Assert.Equal("Shillings One and One Cent Only", result);
    }
}
