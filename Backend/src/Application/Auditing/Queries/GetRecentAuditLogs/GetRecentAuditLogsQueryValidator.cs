using FluentValidation;

namespace Application.Auditing.Queries.GetRecentAuditLogs;

public sealed class GetRecentAuditLogsQueryValidator : AbstractValidator<GetRecentAuditLogsQuery>
{
    public GetRecentAuditLogsQueryValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
    }
}
