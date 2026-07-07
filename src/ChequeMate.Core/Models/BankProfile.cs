namespace ChequeMate.Core.Models;

public class BankProfile
{
    public int Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? Country { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChequeTemplate> Templates { get; set; } = new List<ChequeTemplate>();
}
