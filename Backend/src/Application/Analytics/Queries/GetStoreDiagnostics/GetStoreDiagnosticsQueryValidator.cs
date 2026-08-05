using FluentValidation;

namespace Application.Analytics.Queries.GetStoreDiagnostics;

public sealed class GetStoreDiagnosticsQueryValidator : AbstractValidator<GetStoreDiagnosticsQuery>
{
    public GetStoreDiagnosticsQueryValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
    }
}
