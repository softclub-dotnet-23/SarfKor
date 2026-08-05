namespace Application.Identity.Commands.ConfirmAdminInvitation;

public sealed record ConfirmAdminInvitationCommand(string Email, string Code, string Password);
