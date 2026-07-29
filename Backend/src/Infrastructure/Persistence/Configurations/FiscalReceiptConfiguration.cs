using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class FiscalReceiptConfiguration : IEntityTypeConfiguration<FiscalReceipt>
{
    public void Configure(EntityTypeBuilder<FiscalReceipt> builder)
    {
        builder.HasOne<SaleTransaction>()
            .WithMany()
            .HasForeignKey(x => x.SaleTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
