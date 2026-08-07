using Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StoreEmployeeInvitationConfiguration : IEntityTypeConfiguration<StoreEmployeeInvitation>
{
    public void Configure(EntityTypeBuilder<StoreEmployeeInvitation> builder)
    {
        builder.HasIndex(x => x.TokenHash).IsUnique();
        // The Staff page's "invited but not accepted" list and the create-flow's
        // find-pending-invite-to-reuse both filter on exactly this pair.
        builder.HasIndex(x => new { x.StoreId, x.Email, x.Status });
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
