using Application.Abstractions;
using Application.Common;
using Domain.Stores;
using Domain.ValueObjects;

namespace Application.Stores.Commands.CreateStore;

public sealed class CreateStoreCommandHandler(
    IStoreRepository storeRepository,
    IAuthService authService,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateStoreCommand, CreateStoreResult>
{
    private const string StorePartnerRole = "StorePartner";

    public async Task<CreateStoreResult> Handle(CreateStoreCommand command, CancellationToken cancellationToken)
    {
        var store = new Store
        {
            OwnerUserId = command.OwnerUserId,
            Name = command.Name,
            Address = command.Address,
            Location = new GeoLocation(command.Latitude, command.Longitude),
            // Pending until an Admin approves — hidden from consumer-facing scan/compare/offers
            // in the meantime, but the owner still gets full cabinet access immediately below.
            Status = StoreStatus.PendingApproval,
            ConnectedAt = DateTimeOffset.UtcNow
        };

        storeRepository.Add(store);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Self-service onboarding: creating a store promotes the owner to StorePartner.
        // The caller's current JWT won't carry the new role claim until they log in/refresh again.
        await authService.AssignRoleAsync(command.OwnerUserId, StorePartnerRole, cancellationToken);

        return new CreateStoreResult(store.Id);
    }
}
