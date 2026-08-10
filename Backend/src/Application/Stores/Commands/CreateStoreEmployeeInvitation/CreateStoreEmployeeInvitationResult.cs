namespace Application.Stores.Commands.CreateStoreEmployeeInvitation;

public enum CreateStoreEmployeeInvitationOutcome
{
    Sent,
    StoreNotFound,
    Forbidden,
    AlreadyEmployed,
    SubscriptionInactive
}

public sealed record CreateStoreEmployeeInvitationResult(CreateStoreEmployeeInvitationOutcome Outcome, int? InvitationId);
