using Domain.Stores;

namespace Application.Stores.Queries.GetStoreEmployeeInvitations;

public sealed record GetStoreEmployeeInvitationsQuery(int StoreId, string CallerUserId, StoreEmployeeInvitationStatus? Status);
