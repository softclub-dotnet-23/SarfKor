namespace Application.Assistant;

/// <summary>
/// The assistant's own role model for a chat session — deliberately not the same enum as ASP.NET
/// Identity roles. A Cashier and a store-owning StorePartner both carry the Identity role
/// "StorePartner" (see AddStoreEmployeeCommandHandler), so the assistant re-derives which of the two
/// this caller actually is *for this specific store* via StoreEmployeeRole, not from the JWT role claim.
/// </summary>
public enum AssistantRole
{
    Cashier,
    StorePartner,
    Admin,
}
