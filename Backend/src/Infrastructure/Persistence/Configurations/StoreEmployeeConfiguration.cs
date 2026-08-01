using Domain.Stores;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StoreEmployeeConfiguration : IEntityTypeConfiguration<StoreEmployee>
{
    public void Configure(EntityTypeBuilder<StoreEmployee> builder)
    {
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backstops AddStoreEmployeeCommandHandler's/AcceptStoreEmployeeInvitationCommandHandler's
        // check-then-add — without this, two concurrent adds for the same user could both pass
        // the "not already employed" check and insert duplicate rows.
        builder.HasIndex(x => new { x.StoreId, x.UserId }).IsUnique();
    }
}
