namespace Application.Identity.Commands.InviteAdmin;

public sealed record InviteAdminCommand(string Email, string InvitedByAdminUserId, string? PerformedByIpAddress = null);
