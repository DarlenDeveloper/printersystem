using ChequeMate.Core.Models;

namespace ChequeMate.Core.Services;

public interface IBankProfileService
{
    Task<List<BankProfile>> GetAllAsync();
    Task<BankProfile?> GetByIdAsync(int id);
    Task<BankProfile> CreateAsync(BankProfile profile);
    Task UpdateAsync(BankProfile profile);
    Task DeleteAsync(int id);
}
