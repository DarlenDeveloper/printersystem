using ChequeMate.Core.Models;

namespace ChequeMate.Core.Services;

public interface IChequeService
{
    Task<List<ChequeEntry>> GetPendingEntriesAsync();
    Task<List<ChequeEntry>> GetAllEntriesAsync();
    Task<ChequeEntry?> GetEntryByIdAsync(int id);
    Task<ChequeEntry> CreateEntryAsync(ChequeEntry entry);
    Task UpdateEntryAsync(ChequeEntry entry);
    Task DeleteEntryAsync(int id);
    Task<List<ChequeEntry>> ImportFromCsvAsync(string filePath, int templateId);
}
