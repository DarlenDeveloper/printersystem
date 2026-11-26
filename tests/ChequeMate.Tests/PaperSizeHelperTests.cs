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

    [Theory]
    [InlineData("EPSON L220 Series", 165, 73, true)]
    [InlineData("EPSON L220 Series", 178, 76, true)]
    [InlineData("EPSON L220 Series", 203, 101.6, false)]
    [InlineData("Canon MAXIFY GX7040", 165, 73, false)]
    public void RequiresEpsonL220A4Overlay_AppliesOnlyToUndersizedL220Media(
        string printerName,
        double widthMm,
        double heightMm,
        bool expected)
    {
        Assert.Equal(
            expected,
            PaperSizeHelper.RequiresEpsonL220A4Overlay(printerName, widthMm, heightMm));
    }
}
