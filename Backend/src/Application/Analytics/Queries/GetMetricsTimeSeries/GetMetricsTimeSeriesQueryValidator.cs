using FluentValidation;

namespace Application.Analytics.Queries.GetMetricsTimeSeries;

public sealed class GetMetricsTimeSeriesQueryValidator : AbstractValidator<GetMetricsTimeSeriesQuery>
{
    public GetMetricsTimeSeriesQueryValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From);
        RuleFor(x => x).Must(x => x.To.DayNumber - x.From.DayNumber <= 366)
            .WithMessage("Range cannot exceed 366 days.");
    }
}
