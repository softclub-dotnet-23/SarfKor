namespace Application.Identity.Commands.UnblockUser;

public enum UnblockUserOutcome
{
    Unblocked,
    NotFound
}

public sealed record UnblockUserResult(UnblockUserOutcome Outcome);
