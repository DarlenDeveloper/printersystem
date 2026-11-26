using ChequeMate.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

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
    public ICollection<PrintHistory> PrintHistories { get; set; } = new List<PrintHistory>();

    [NotMapped]
    public string? LastPrintError
    {
        get
        {
            var latestAttempt = PrintHistories
                .OrderByDescending(history => history.PrintedAt)
                .ThenByDescending(history => history.Id)
                .FirstOrDefault();
            return latestAttempt is { Success: false }
                ? latestAttempt.ErrorMessage ?? "The print attempt failed without a driver message."
                : null;
        }
    }
}
