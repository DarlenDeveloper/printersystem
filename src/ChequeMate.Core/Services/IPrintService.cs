using ChequeMate.Core.Models;

namespace ChequeMate.Core.Services;

public interface IPrintService
{
    /// <summary>
    /// Prints a single cheque entry using its associated template.
    /// </summary>
    Task<PrintHistory> PrintChequeAsync(ChequeEntry entry, string? printerName = null);

    /// <summary>
    /// Prints multiple cheque entries sequentially.
    /// </summary>
    Task<List<PrintHistory>> PrintBatchAsync(List<ChequeEntry> entries, string? printerName = null);

    /// <summary>
    /// Prints an alignment test page for calibration.
    /// </summary>
    Task PrintAlignmentTestAsync(ChequeTemplate template, string? printerName = null);

    /// <summary>
    /// Gets available printer names on the system.
    /// </summary>
    List<string> GetAvailablePrinters();
}
