using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        // Backstops CreateBrandCommandHandler's check-then-add — no configuration existed for
        // this entity at all before, so nothing enforced uniqueness at the DB level.
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
