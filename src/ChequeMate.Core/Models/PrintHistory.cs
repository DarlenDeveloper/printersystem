namespace ChequeMate.Core.Models;

public class PrintHistory
{
    public int Id { get; set; }
    public int ChequeEntryId { get; set; }
    public DateTime PrintedAt { get; set; } = DateTime.UtcNow;
    public string? PrinterName { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public ChequeEntry? ChequeEntry { get; set; }
}
