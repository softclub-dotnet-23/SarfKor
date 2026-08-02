using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReturnLineItemConfiguration : IEntityTypeConfiguration<ReturnLineItem>
{
    public void Configure(EntityTypeBuilder<ReturnLineItem> builder)
    {
        builder.ComplexProperty(x => x.RefundAmount, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        // Restrict, not Cascade — same reasoning as SaleLineItemConfiguration: a return is a
        // financial/audit record, its lines shouldn't silently vanish if the return is ever deleted.
        builder.HasOne<SaleReturn>()
            .WithMany(r => r.Lines)
            .HasForeignKey(x => x.SaleReturnId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SaleLineItem>()
            .WithMany()
            .HasForeignKey(x => x.SaleLineItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
