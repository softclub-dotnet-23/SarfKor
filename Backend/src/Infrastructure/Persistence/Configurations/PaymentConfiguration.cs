using Domain.Payments;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ComplexProperty(x => x.Amount, b => b.Property(m => m.Amount).HasPrecision(18, 2));
        builder.ComplexProperty(x => x.ProviderToken);
        builder.HasOne<SaleTransaction>()
            .WithMany()
            .HasForeignKey(x => x.SaleTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
