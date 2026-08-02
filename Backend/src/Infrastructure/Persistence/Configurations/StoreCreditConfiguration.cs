using Domain.Payments;
using Domain.Customers;
using Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StoreCreditConfiguration : IEntityTypeConfiguration<StoreCredit>
{
    public void Configure(EntityTypeBuilder<StoreCredit> builder)
    {
        builder.ComplexProperty(x => x.Balance, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backstops IssueStoreCreditCommandHandler's check-then-insert — without this, two
        // concurrent first-time issues for the same customer can each pass the check and insert
        // a duplicate row, silently splitting the customer's balance across two rows forever.
        builder.HasIndex(x => new { x.StoreId, x.CustomerId }).IsUnique();
    }
}
