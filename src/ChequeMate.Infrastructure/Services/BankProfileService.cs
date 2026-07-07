using ChequeMate.Core.Models;
using ChequeMate.Core.Services;
using ChequeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChequeMate.Infrastructure.Services;

public class BankProfileService : IBankProfileService
{
    private readonly AppDbContext _db;

    public BankProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<BankProfile>> GetAllAsync()
    {
        return await _db.BankProfiles.OrderBy(b => b.BankName).ToListAsync();
    }

    public async Task<BankProfile?> GetByIdAsync(int id)
    {
        return await _db.BankProfiles.FindAsync(id);
    }

    public async Task<BankProfile> CreateAsync(BankProfile profile)
    {
        profile.CreatedAt = DateTime.UtcNow;
        _db.BankProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task UpdateAsync(BankProfile profile)
    {
        _db.BankProfiles.Update(profile);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var profile = await _db.BankProfiles.FindAsync(id);
        if (profile != null)
        {
            _db.BankProfiles.Remove(profile);
            await _db.SaveChangesAsync();
        }
    }
}
