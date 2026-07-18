using FluentValidation;

namespace Application.Feedback.Commands.ModerateReport;

public sealed class ModerateReportCommandValidator : AbstractValidator<ModerateReportCommand>
{
    public ModerateReportCommandValidator()
    {
        RuleFor(x => x.ReportId).GreaterThan(0);
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
