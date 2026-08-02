namespace Application.Stores.Commands.ApproveStore;

public enum ApproveStoreOutcome
{
    Approved,
    NotFound,
    AlreadyApproved
}

public sealed record ApproveStoreResult(ApproveStoreOutcome Outcome);
