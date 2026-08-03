namespace Application.Stores.Commands.UpdateStore;

public sealed record UpdateStoreCommand(int StoreId, string RequestedByUserId, string Name, string Address, double Latitude, double Longitude);
