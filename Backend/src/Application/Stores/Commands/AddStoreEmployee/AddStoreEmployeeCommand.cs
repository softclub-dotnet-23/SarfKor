using Domain.Stores;

namespace Application.Stores.Commands.AddStoreEmployee;

public sealed record AddStoreEmployeeCommand(int StoreId, string EmployeeEmail, StoreEmployeeRole Role, string PerformedByUserId);
