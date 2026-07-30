namespace Application.Identity.Commands.ResetPassword;

public enum ResetPasswordOutcome
{
    Reset,
    Failed
}

public sealed record ResetPasswordResult(ResetPasswordOutcome Outcome);
