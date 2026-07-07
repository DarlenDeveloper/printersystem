using ChequeMate.Core.Models;

namespace ChequeMate.Core.Services;

public interface ITemplateService
{
    Task<List<ChequeTemplate>> GetAllTemplatesAsync();
    Task<ChequeTemplate?> GetTemplateByIdAsync(int id);
    Task<List<ChequeTemplate>> GetTemplatesByBankAsync(int bankProfileId);
    Task<ChequeTemplate> CreateTemplateAsync(ChequeTemplate template);
    Task UpdateTemplateAsync(ChequeTemplate template);
    Task DeleteTemplateAsync(int id);
}
