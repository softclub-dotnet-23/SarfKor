using Domain.Stores;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StoreOwnerInvitationConfiguration : IEntityTypeConfiguration<StoreOwnerInvitation>
{
    public void Configure(EntityTypeBuilder<StoreOwnerInvitation> builder)
    {
        builder.ComplexProperty(x => x.Location);
        builder.HasIndex(x => x.Email);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
