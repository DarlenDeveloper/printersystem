using ChequeMate.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChequeMate.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<BankProfile> BankProfiles => Set<BankProfile>();
    public DbSet<ChequeTemplate> ChequeTemplates => Set<ChequeTemplate>();
    public DbSet<TemplateField> TemplateFields => Set<TemplateField>();
    public DbSet<ChequeEntry> ChequeEntries => Set<ChequeEntry>();
    public DbSet<PrintHistory> PrintHistories => Set<PrintHistory>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BankProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BankName).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<ChequeTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.BankProfile)
                  .WithMany(b => b.Templates)
                  .HasForeignKey(e => e.BankProfileId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TemplateField>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FieldType).HasConversion<string>();
            entity.Property(e => e.Alignment).HasConversion<string>();
            entity.HasOne(e => e.Template)
                  .WithMany(t => t.Fields)
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChequeEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Payee).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AmountWords).IsRequired();
            entity.Property(e => e.PrintStatus).HasConversion<string>();
            entity.HasOne(e => e.Template)
                  .WithMany()
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrintHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.ChequeEntry)
                  .WithMany()
                  .HasForeignKey(e => e.ChequeEntryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
