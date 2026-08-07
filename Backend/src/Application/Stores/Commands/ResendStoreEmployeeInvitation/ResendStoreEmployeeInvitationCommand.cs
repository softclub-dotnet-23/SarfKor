namespace Application.Stores.Commands.ResendStoreEmployeeInvitation;

public sealed record ResendStoreEmployeeInvitationCommand(int InvitationId, string PerformedByUserId);
