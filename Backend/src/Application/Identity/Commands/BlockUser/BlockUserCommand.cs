namespace Application.Identity.Commands.BlockUser;

public sealed record BlockUserCommand(string UserId, string Reason, string PerformedByAdminUserId, string? PerformedByIpAddress = null);
