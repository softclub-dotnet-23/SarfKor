using FluentValidation;

namespace Application.Sales.Queries.GetProfitReport;

public sealed class GetProfitReportQueryValidator : AbstractValidator<GetProfitReportQuery>
{
    public GetProfitReportQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
    }
}
