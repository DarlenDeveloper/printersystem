using ChequeMate.Core.Enums;

namespace ChequeMate.Core.Models;

public class ChequeEntry
{
    public int Id { get; set; }
    public int TemplateId { get; set; }

    public string Payee { get; set; } = string.Empty;
    public decimal AmountFigures { get; set; }
    public string AmountWords { get; set; } = string.Empty;
    public DateTime ChequeDate { get; set; } = DateTime.Today;
    public string? ChequeNumber { get; set; }
    public string? Memo { get; set; }

    public PrintStatus PrintStatus { get; set; } = PrintStatus.Pending;
    public DateTime? PrintedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChequeTemplate? Template { get; set; }
}
