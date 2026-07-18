using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SaleTransactionConfiguration : IEntityTypeConfiguration<SaleTransaction>
{
    public void Configure(EntityTypeBuilder<SaleTransaction> builder)
    {
        builder.HasIndex(x => new { x.StoreId, x.IdempotencyKey }).IsUnique();
    }
}
