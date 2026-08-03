namespace Application.Identity.Commands.ChangePassword;

public enum ChangePasswordOutcome
{
    Changed,
    WrongCurrentPassword,
    UserNotFound
}

public sealed record ChangePasswordResult(ChangePasswordOutcome Outcome);
