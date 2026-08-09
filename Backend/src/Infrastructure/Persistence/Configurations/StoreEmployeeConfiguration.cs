using Domain.Stores;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StoreEmployeeConfiguration : IEntityTypeConfiguration<StoreEmployee>
{
    public void Configure(EntityTypeBuilder<StoreEmployee> builder)
    {
        // IsRequired(false) is load-bearing, not decorative: MonthlySalary is `Money?`, and without
        // it EF's complex-property mapping silently treats a null MonthlySalary as "required but
        // unset" on INSERT -- it writes Amount=0 (decimal default) while leaving Currency NULL
        // (string default) instead of persisting a clean all-NULL row. The next read then tries to
        // materialize Money(0, null) and Money's own constructor throws ("Currency must be a
        // 3-letter code"), 500ing GetMyStores/GetStoreEmployees for any employee whose salary was
        // never set -- e.g. every fresh AcceptStoreEmployeeInvitationCommandHandler hire. Found via
        // this session's live invite-acceptance verification (see WORKLOG); StoreEmployeeRepository
        // .GetRoleAsync already had a comment flagging this as a known, not-yet-fixed gap.
        builder.ComplexProperty(x => x.MonthlySalary, b =>
        {
            b.IsRequired(false);
            b.Property(m => m.Amount).HasPrecision(18, 2);
        });

        // Explicit DB-level default -- without it every pre-existing row would migrate to NULL/false
        // instead of true, silently "disabling" every employee hired before this column existed.
        builder.Property(x => x.IsActive).HasDefaultValue(true);

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
