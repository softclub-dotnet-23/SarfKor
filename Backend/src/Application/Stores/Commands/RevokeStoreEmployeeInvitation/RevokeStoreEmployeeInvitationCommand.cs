namespace Application.Stores.Commands.RevokeStoreEmployeeInvitation;

public sealed record RevokeStoreEmployeeInvitationCommand(int InvitationId, string PerformedByUserId);
