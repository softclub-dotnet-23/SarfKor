using Domain.Stores;

namespace Application.Stores.Commands.AddStoreEmployee;

public sealed record AddStoreEmployeeCommand(int StoreId, string EmployeeUserId, StoreEmployeeRole Role, string PerformedByUserId);
