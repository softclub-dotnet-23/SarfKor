using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductSubmissionConfiguration : IEntityTypeConfiguration<ProductSubmission>
{
    public void Configure(EntityTypeBuilder<ProductSubmission> builder)
    {
        builder.ComplexProperty(x => x.Barcode);
    }
}
