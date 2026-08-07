using Domain.Products;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // OwnsOne, not ComplexProperty — needed so the nested Value column can carry an index;
        // was completely unindexed — every barcode scan (the single hottest, fully public
        // endpoint) did a full table scan; unique also closes a duplicate-barcode gap the
        // check-then-act submission flow relied on the app layer alone to prevent.
        builder.OwnsOne(x => x.Barcode, b =>
        {
            b.Property(v => v.Value).HasColumnName("Barcode_Value");
            b.HasIndex(v => v.Value).IsUnique();
        });
        builder.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaxRate>()
            .WithMany()
            .HasForeignKey(x => x.TaxRateId)
            .OnDelete(DeleteBehavior.SetNull);

        // SearchProductsQueryHandler's `ILIKE '%term%'` on Name — a plain btree index only helps a
        // prefix match, not an arbitrary substring; gin_trgm_ops does. CategoryId/BrandId already
        // get a btree index for free from the FK configuration above.
        builder.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
