using FluentValidation;

namespace Application.Feedback.Commands.RaiseReportDispute;

public sealed class RaiseReportDisputeCommandValidator : AbstractValidator<RaiseReportDisputeCommand>
{
    public RaiseReportDisputeCommandValidator()
    {
        RuleFor(x => x.ReportId).GreaterThan(0);
        RuleFor(x => x.DisputedByUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
