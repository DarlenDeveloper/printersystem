using ChequeMate.Core.Models;
using ChequeMate.Core.Services;
using ChequeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChequeMate.Infrastructure.Services;

public class TemplateService : ITemplateService
{
    private readonly AppDbContext _db;

    public TemplateService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ChequeTemplate>> GetAllTemplatesAsync()
    {
        return await _db.ChequeTemplates
            .Include(t => t.BankProfile)
            .Include(t => t.Fields)
            .OrderBy(t => t.TemplateName)
            .ToListAsync();
    }

    public async Task<ChequeTemplate?> GetTemplateByIdAsync(int id)
    {
        return await _db.ChequeTemplates
            .Include(t => t.BankProfile)
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<ChequeTemplate>> GetTemplatesByBankAsync(int bankProfileId)
    {
        return await _db.ChequeTemplates
            .Include(t => t.Fields)
            .Where(t => t.BankProfileId == bankProfileId)
            .ToListAsync();
    }

    public async Task<ChequeTemplate> CreateTemplateAsync(ChequeTemplate template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        _db.ChequeTemplates.Add(template);
        await _db.SaveChangesAsync();
        return template;
    }

    public async Task UpdateTemplateAsync(ChequeTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;

        var existing = await _db.ChequeTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == template.Id);

        if (existing == null) return;

        _db.Entry(existing).CurrentValues.SetValues(template);

        // Remove deleted fields
        foreach (var existingField in existing.Fields.ToList())
        {
            if (!template.Fields.Any(f => f.Id == existingField.Id))
                _db.TemplateFields.Remove(existingField);
        }

        // Update or add fields
        foreach (var field in template.Fields)
        {
            var existingField = existing.Fields.FirstOrDefault(f => f.Id == field.Id);
            if (existingField != null)
                _db.Entry(existingField).CurrentValues.SetValues(field);
            else
                existing.Fields.Add(field);
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteTemplateAsync(int id)
    {
        var template = await _db.ChequeTemplates.FindAsync(id);
        if (template != null)
        {
            _db.ChequeTemplates.Remove(template);
            await _db.SaveChangesAsync();
        }
    }
}
