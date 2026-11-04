using ChequeMate.Infrastructure.Printing;

namespace ChequeMate.Tests;

public class PaperSizeHelperTests
{
    [Fact]
    public void CreateChequePaperSize_NormalizesToPortraitBaseSize()
    {
        var size = PaperSizeHelper.CreateChequePaperSize(200, 90);

        Assert.Equal(354, size.Width);
        Assert.Equal(787, size.Height);
    }

    [Theory]
    [InlineData(25.4, 100)]
    [InlineData(89, 350)]
    [InlineData(210, 827)]
    public void MmToHundredthsOfInch_ConvertsPhysicalUnits(double mm, int expected)
    {
        Assert.Equal(expected, PaperSizeHelper.MmToHundredthsOfInch(mm));
    }
}
