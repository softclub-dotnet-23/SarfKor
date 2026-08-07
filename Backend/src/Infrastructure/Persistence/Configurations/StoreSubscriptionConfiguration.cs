using Domain.Stores;
using Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StoreSubscriptionConfiguration : IEntityTypeConfiguration<StoreSubscription>
{
    public void Configure(EntityTypeBuilder<StoreSubscription> builder)
    {
        builder.ComplexProperty(x => x.PriceAtIssue, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasIndex(x => x.StoreId).IsUnique();
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
