namespace Application.Stores.Commands.AcceptStoreEmployeeInvitation;

/// <summary>Password is null/empty when the invitee already has an account (resolved inside the
/// handler, not knowable at validation time) — the accept page only collects it when the public
/// GetStoreEmployeeInvitationByTokenQuery said RequiresPassword. DisplayName is always collected;
/// for an already-existing account it's simply not applied (their profile is left alone).</summary>
public sealed record AcceptStoreEmployeeInvitationCommand(string Token, string DisplayName, string? Password);
