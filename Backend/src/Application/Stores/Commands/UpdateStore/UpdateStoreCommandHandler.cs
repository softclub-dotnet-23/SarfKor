using Application.Abstractions;
using Application.Common;
using Domain.ValueObjects;

namespace Application.Stores.Commands.UpdateStore;

public sealed class UpdateStoreCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateStoreCommand, UpdateStoreResult>
{
    public async Task<UpdateStoreResult> Handle(UpdateStoreCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new UpdateStoreResult(UpdateStoreOutcome.StoreNotFound);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.RequestedByUserId, cancellationToken))
            return new UpdateStoreResult(UpdateStoreOutcome.Forbidden);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new UpdateStoreResult(UpdateStoreOutcome.SubscriptionInactive);

        store.Name = command.Name;
        store.Address = command.Address;
        store.Location = new GeoLocation(command.Latitude, command.Longitude);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateStoreResult(UpdateStoreOutcome.Updated);
    }
}
