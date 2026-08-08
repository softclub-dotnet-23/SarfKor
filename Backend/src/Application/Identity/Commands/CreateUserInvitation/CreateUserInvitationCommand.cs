namespace Application.Identity.Commands.CreateUserInvitation;

/// <summary>/admin/users' "Добавить пользователя" — invites someone by email into any of the
/// three platform Identity roles, reusing the exact same StoreEmployeeInvitation mechanism
/// StaffPage's cashier/owner invites already run on (see StoreEmployeeInvitation's own doc
/// comment). StoreId is required iff InvitedRole is "StorePartner" (the invitee becomes that
/// store's Owner) and must be null otherwise — enforced by the validator, not just the caller.</summary>
public sealed record CreateUserInvitationCommand(string Email, string InvitedRole, int? StoreId, string PerformedByUserId, string? PerformedByIpAddress = null);
