namespace Application.Stores.Commands.AdminCreateStorePartner;

public sealed record AdminCreateStorePartnerCommand(
    string AdminUserId,
    string Email,
    string StoreName,
    string Address,
    double Latitude,
    double Longitude,
    string? PerformedByIpAddress = null);
