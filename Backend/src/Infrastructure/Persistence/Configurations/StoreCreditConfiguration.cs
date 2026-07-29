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
    }
}
