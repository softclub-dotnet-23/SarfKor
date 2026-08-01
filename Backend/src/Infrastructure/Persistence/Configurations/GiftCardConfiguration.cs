using Domain.Payments;
using Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class GiftCardConfiguration : IEntityTypeConfiguration<GiftCard>
{
    public void Configure(EntityTypeBuilder<GiftCard> builder)
    {
        builder.ComplexProperty(x => x.Balance, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.IssuingStoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
