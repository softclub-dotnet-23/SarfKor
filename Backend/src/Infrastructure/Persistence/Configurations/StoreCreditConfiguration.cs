using Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StoreCreditConfiguration : IEntityTypeConfiguration<StoreCredit>
{
    public void Configure(EntityTypeBuilder<StoreCredit> builder)
    {
        builder.ComplexProperty(x => x.Balance);
    }
}
