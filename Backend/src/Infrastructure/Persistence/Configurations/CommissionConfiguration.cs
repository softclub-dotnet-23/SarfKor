using Domain.Sales;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> builder)
    {
        builder.ComplexProperty(x => x.Amount, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CashierUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SaleTransaction>()
            .WithMany()
            .HasForeignKey(x => x.SaleTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
