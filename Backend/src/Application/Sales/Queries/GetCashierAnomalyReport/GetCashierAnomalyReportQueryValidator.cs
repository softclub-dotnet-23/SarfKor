using FluentValidation;

namespace Application.Sales.Queries.GetCashierAnomalyReport;

public sealed class GetCashierAnomalyReportQueryValidator : AbstractValidator<GetCashierAnomalyReportQuery>
{
    public GetCashierAnomalyReportQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
    }
}
