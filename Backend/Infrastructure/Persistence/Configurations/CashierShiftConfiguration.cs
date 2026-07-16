using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CashierShiftConfiguration : IEntityTypeConfiguration<CashierShift>
{
    public void Configure(EntityTypeBuilder<CashierShift> builder)
    {
        builder.ComplexProperty(x => x.OpeningCash);
        builder.ComplexProperty(x => x.ExpectedCash);
        builder.ComplexProperty(x => x.ClosingCash);
    }
}
