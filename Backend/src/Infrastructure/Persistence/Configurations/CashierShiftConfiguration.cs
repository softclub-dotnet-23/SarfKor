using Domain.Sales;
using Infrastructure.Identity;
using Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CashierShiftConfiguration : IEntityTypeConfiguration<CashierShift>
{
    public void Configure(EntityTypeBuilder<CashierShift> builder)
    {
        builder.ComplexProperty(x => x.OpeningCash, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.ComplexProperty(x => x.ExpectedCash, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.ComplexProperty(x => x.ClosingCash, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CashierUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
