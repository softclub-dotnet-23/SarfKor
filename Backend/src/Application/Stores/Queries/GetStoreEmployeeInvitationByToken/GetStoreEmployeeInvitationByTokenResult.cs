using Domain.Stores;

namespace Application.Stores.Queries.GetStoreEmployeeInvitationByToken;

public enum GetStoreEmployeeInvitationByTokenOutcome
{
    Valid,
    NotFound,
    Expired,
    Accepted,
    Revoked
}

/// <summary>InvitedRole/StoreName/Email/Role/RequiresPassword are only meaningful when Outcome is
/// Valid. InvitedRole is "User" | "StorePartner" | "Admin" — the Identity role accepting this
/// invite grants; StoreName/Role (the Owner/Cashier sub-role) are only set when InvitedRole is
/// StorePartner. RequiresPassword is false when an account for this email already exists — the
/// accept page skips the password fields entirely in that case (task spec: "Пароль в этом случае
/// не запрашивается").</summary>
public sealed record GetStoreEmployeeInvitationByTokenResult(
    GetStoreEmployeeInvitationByTokenOutcome Outcome,
    string? InvitedRole,
    string? StoreName,
    string? Email,
    StoreEmployeeRole? Role,
    bool RequiresPassword);
