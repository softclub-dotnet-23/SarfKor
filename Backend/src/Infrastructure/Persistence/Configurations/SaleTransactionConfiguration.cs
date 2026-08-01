using Domain.Payments;
using Domain.Sales;
using Domain.Stores;
using Domain.Customers;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SaleTransactionConfiguration : IEntityTypeConfiguration<SaleTransaction>
{
    public void Configure(EntityTypeBuilder<SaleTransaction> builder)
    {
        builder.HasIndex(x => new { x.StoreId, x.IdempotencyKey }).IsUnique();
        builder.Property(x => x.GiftCardAmountApplied).HasPrecision(18, 2);
        builder.Property(x => x.StoreCreditAmountApplied).HasPrecision(18, 2);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CashierUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.VoidedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<GiftCard>()
            .WithMany()
            .HasForeignKey(x => x.GiftCardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
