using Microsoft.EntityFrameworkCore;
using PurchaseTransactionAPI.Models;

namespace PurchaseTransactionAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<PurchaseTransaction> PurchaseTransactions => Set<PurchaseTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseTransaction>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.PurchaseAmountUsd)
                .HasPrecision(18, 2)
                .IsRequired();
        });
    }
}
