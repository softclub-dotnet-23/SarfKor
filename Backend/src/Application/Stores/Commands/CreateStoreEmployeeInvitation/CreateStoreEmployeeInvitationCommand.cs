using Domain.Stores;

namespace Application.Stores.Commands.CreateStoreEmployeeInvitation;

public sealed record CreateStoreEmployeeInvitationCommand(int StoreId, string Email, StoreEmployeeRole Role, string PerformedByUserId);
