using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReturnLineItemConfiguration : IEntityTypeConfiguration<ReturnLineItem>
{
    public void Configure(EntityTypeBuilder<ReturnLineItem> builder)
    {
        builder.ComplexProperty(x => x.RefundAmount);
    }
}
