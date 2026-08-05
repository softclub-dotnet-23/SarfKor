using Domain.Subscriptions;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ComplexProperty(x => x.Amount, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasOne<StoreSubscription>()
            .WithMany()
            .HasForeignKey(x => x.StoreSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        // Self-referencing reversal link — Restrict, not Cascade: deleting a payment (which never
        // happens; payments are immutable/append-only) must never be able to cascade-delete the
        // payment it reversed.
        builder.HasOne<SubscriptionPayment>()
            .WithMany()
            .HasForeignKey(x => x.ReversedPaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
