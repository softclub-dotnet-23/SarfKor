using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CostPriceConfiguration : IEntityTypeConfiguration<CostPrice>
{
    public void Configure(EntityTypeBuilder<CostPrice> builder)
    {
        builder.ComplexProperty(x => x.Amount);
    }
}
