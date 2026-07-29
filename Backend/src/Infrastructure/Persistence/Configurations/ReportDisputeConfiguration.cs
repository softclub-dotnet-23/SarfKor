using Domain.Feedback;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReportDisputeConfiguration : IEntityTypeConfiguration<ReportDispute>
{
    public void Configure(EntityTypeBuilder<ReportDispute> builder)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.DisputedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Report>()
            .WithMany()
            .HasForeignKey(x => x.ReportId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
