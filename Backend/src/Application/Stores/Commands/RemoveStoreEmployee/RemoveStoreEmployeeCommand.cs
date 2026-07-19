namespace Application.Stores.Commands.RemoveStoreEmployee;

public sealed record RemoveStoreEmployeeCommand(int StoreEmployeeId, string PerformedByUserId);
