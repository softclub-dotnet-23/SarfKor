using Domain.Assistant;
using Domain.Stores;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PendingAssistantActionConfiguration : IEntityTypeConfiguration<PendingAssistantAction>
{
    public void Configure(EntityTypeBuilder<PendingAssistantAction> builder)
    {
        builder.Property(x => x.ParametersJson).IsRequired();
        builder.Property(x => x.Summary).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        // ConfirmAssistantActionCommandHandler always looks up by Id directly, not this -- indexed
        // for the (rare, admin-facing) "list my recent proposals" query this data shape invites later.
        builder.HasIndex(x => new { x.RequestedByUserId, x.ConfirmedAt });
    }
}
