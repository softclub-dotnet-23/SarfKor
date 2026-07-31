using Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StoreEmployeeInvitationConfiguration : IEntityTypeConfiguration<StoreEmployeeInvitation>
{
    public void Configure(EntityTypeBuilder<StoreEmployeeInvitation> builder)
    {
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
