using Domain.Receipts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReceiptLineItemConfiguration : IEntityTypeConfiguration<ReceiptLineItem>
{
    public void Configure(EntityTypeBuilder<ReceiptLineItem> builder)
    {
        builder.ComplexProperty(x => x.Price);
    }
}
