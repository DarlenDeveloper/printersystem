using ChequeMate.Core.Models;

namespace ChequeMate.Core.Services;

public interface IPrintService
{
    /// <summary>
    /// Prints a single cheque entry using its associated template.
    /// </summary>
    Task<PrintHistory> PrintChequeAsync(
        ChequeEntry entry,
        string printerName,
        string? paperSourceName,
        PrintJobOptions options);

    /// <summary>
    /// Prints an alignment test page for calibration.
    /// </summary>
    Task PrintAlignmentTestAsync(
        ChequeTemplate template,
        string printerName,
        string? paperSourceName,
        PrintJobOptions options);

    /// <summary>
    /// Gets available printer names on the system.
    /// </summary>
    List<string> GetAvailablePrinters();

    /// <summary>
    /// Gets the physical paper sources exposed by a printer driver.
    /// </summary>
    List<string> GetAvailablePaperSources(string printerName);
}
