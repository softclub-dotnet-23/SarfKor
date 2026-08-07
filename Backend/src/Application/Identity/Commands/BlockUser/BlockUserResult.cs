namespace Application.Identity.Commands.BlockUser;

public enum BlockUserOutcome
{
    Blocked,
    NotFound
}

public sealed record BlockUserResult(BlockUserOutcome Outcome);
