using ChequeMate.Core.Models;

namespace ChequeMate.Core.Services;

public interface IPrinterProfileService
{
    PrinterProfile GetOrCreate(string printerName, double chequeWidthMm, double chequeHeightMm);
    void Save(PrinterProfile profile);
    void Clear(string printerName, double chequeWidthMm, double chequeHeightMm);
}
