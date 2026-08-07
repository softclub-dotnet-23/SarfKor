namespace Application.Identity.Commands.UnblockUser;

public sealed record UnblockUserCommand(string UserId, string Reason, string PerformedByAdminUserId, string? PerformedByIpAddress = null);
