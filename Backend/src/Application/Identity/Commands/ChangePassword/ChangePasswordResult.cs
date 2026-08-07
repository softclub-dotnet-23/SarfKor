namespace Application.Identity.Commands.ChangePassword;

public enum ChangePasswordOutcome
{
    Succeeded,
    IncorrectCurrentPassword,
    WeakPassword,
    NotFound
}

public sealed record ChangePasswordResult(ChangePasswordOutcome Outcome, IReadOnlyList<string> Errors);
