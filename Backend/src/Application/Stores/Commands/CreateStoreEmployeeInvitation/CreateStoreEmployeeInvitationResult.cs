namespace Application.Stores.Commands.CreateStoreEmployeeInvitation;

public enum CreateStoreEmployeeInvitationOutcome
{
    Sent,
    StoreNotFound,
    Forbidden,
    AlreadyEmployed
}

public sealed record CreateStoreEmployeeInvitationResult(CreateStoreEmployeeInvitationOutcome Outcome, int? InvitationId);
