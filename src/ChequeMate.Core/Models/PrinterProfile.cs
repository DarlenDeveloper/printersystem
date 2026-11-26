namespace ChequeMate.Core.Models;

public sealed class PrinterProfile
{
    public string PrinterName { get; set; } = string.Empty;
    public double ChequeWidthMm { get; set; }
    public double ChequeHeightMm { get; set; }
    public string? PaperSourceName { get; set; }
    public PrintJobOptions Options { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}
