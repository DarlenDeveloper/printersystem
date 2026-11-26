using ChequeMate.Core.Enums;

namespace ChequeMate.Core.Models;

public sealed class PrintJobOptions
{
    public PrintMode Mode { get; set; } = PrintMode.A4Positioned;
    public FeedOrientation FeedOrientation { get; set; } = FeedOrientation.LongEdgeFirst;
    public PaperAlignment PaperAlignment { get; set; } = PaperAlignment.Right;
    public double OffsetXMm { get; set; }
    public double OffsetYMm { get; set; }
}
