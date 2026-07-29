using Domain.Offers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ExpiringOfferConfiguration : IEntityTypeConfiguration<ExpiringOffer>
{
    public void Configure(EntityTypeBuilder<ExpiringOffer> builder)
    {
        builder.ComplexProperty(x => x.OriginalPrice);
        builder.ComplexProperty(x => x.DiscountedPrice);
    }
}
