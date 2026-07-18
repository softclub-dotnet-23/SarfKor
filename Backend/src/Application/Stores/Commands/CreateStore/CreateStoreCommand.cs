namespace Application.Stores.Commands.CreateStore;

public sealed record CreateStoreCommand(string OwnerUserId, string Name, string Address, double Latitude, double Longitude);
