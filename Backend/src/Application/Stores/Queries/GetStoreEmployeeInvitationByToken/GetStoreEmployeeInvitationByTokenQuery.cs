namespace Application.Stores.Queries.GetStoreEmployeeInvitationByToken;

/// <summary>Public, unauthenticated — backs the /invite/{token} registration page's "who's
/// inviting me to what" context panel, before the invitee has committed to anything.</summary>
public sealed record GetStoreEmployeeInvitationByTokenQuery(string Token);
