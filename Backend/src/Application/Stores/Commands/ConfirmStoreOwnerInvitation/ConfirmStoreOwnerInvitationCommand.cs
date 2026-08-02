namespace Application.Stores.Commands.ConfirmStoreOwnerInvitation;

public sealed record ConfirmStoreOwnerInvitationCommand(string Email, string Code, string Password);
