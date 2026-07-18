using FluentValidation;

namespace Application.Feedback.Commands.ResolveReportDispute;

public sealed class ResolveReportDisputeCommandValidator : AbstractValidator<ResolveReportDisputeCommand>
{
    public ResolveReportDisputeCommandValidator()
    {
        RuleFor(x => x.DisputeId).GreaterThan(0);
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
