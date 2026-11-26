using ChequeMate.Core.Enums;
using ChequeMate.Core.Models;
using ChequeMate.Infrastructure.Printing;

namespace ChequeMate.Tests;

public class ChequePageLayoutCalculatorTests
{
    private static readonly ChequeTemplate Corporate = new()
    {
        PaperWidthMm = 203,
        PaperHeightMm = 101.6
    };

    [Fact]
    public void A4Positioned_LongEdgeFirst_RightAlignsChequeWithoutCustomPaper()
    {
        var layout = ChequePageLayoutCalculator.Calculate(Corporate, new PrintJobOptions
        {
            Mode = PrintMode.A4Positioned,
            FeedOrientation = FeedOrientation.LongEdgeFirst,
            PaperAlignment = PaperAlignment.Right
        });

        Assert.Equal(210, layout.PageWidthMm);
        Assert.Equal(297, layout.PageHeightMm);
        Assert.Equal(7, layout.OriginXMm, 3);
        Assert.Equal(0, layout.OriginYMm);
        Assert.False(layout.RotateClockwise);
    }

    [Fact]
    public void A4Positioned_ShortEdgeFirst_RotatesAndRightAlignsOccupiedArea()
    {
        var layout = ChequePageLayoutCalculator.Calculate(Corporate, new PrintJobOptions
        {
            Mode = PrintMode.A4Positioned,
            FeedOrientation = FeedOrientation.ShortEdgeFirst,
            PaperAlignment = PaperAlignment.Right
        });

        Assert.Equal(108.4, layout.OriginXMm, 3);
        Assert.Equal(101.6, layout.OccupiedWidthMm, 3);
        Assert.Equal(203, layout.OccupiedHeightMm);
        Assert.True(layout.RotateClockwise);
    }

    [Fact]
    public void ExactSize_UsesFeedOrientedDimensionsWithoutRegisteredForm()
    {
        var layout = ChequePageLayoutCalculator.Calculate(Corporate, new PrintJobOptions
        {
            Mode = PrintMode.ExactChequeSize,
            FeedOrientation = FeedOrientation.LongEdgeFirst,
            OffsetXMm = 1.25,
            OffsetYMm = -0.5
        });

        Assert.Equal(203, layout.PageWidthMm);
        Assert.Equal(101.6, layout.PageHeightMm, 3);
        Assert.Equal(1.25, layout.OriginXMm, 3);
        Assert.Equal(-0.5, layout.OriginYMm, 3);
    }

    [Fact]
    public void A4Positioned_CentreAlignmentUsesEqualSideSpace()
    {
        var layout = ChequePageLayoutCalculator.Calculate(Corporate, new PrintJobOptions
        {
            Mode = PrintMode.A4Positioned,
            PaperAlignment = PaperAlignment.Centre
        });

        Assert.Equal(3.5, layout.OriginXMm, 3);
    }
}
