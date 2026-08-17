using Application.Abstractions;
using Application.Common;
using Domain.Loyalty;

namespace Application.Loyalty.Commands.CreateLoyaltyProgram;

public sealed class CreateLoyaltyProgramCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ILoyaltyProgramRepository loyaltyProgramRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateLoyaltyProgramCommand, CreateLoyaltyProgramResult>
{
    public async Task<CreateLoyaltyProgramResult> Handle(CreateLoyaltyProgramCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new CreateLoyaltyProgramResult(CreateLoyaltyProgramOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new CreateLoyaltyProgramResult(CreateLoyaltyProgramOutcome.Forbidden, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new CreateLoyaltyProgramResult(CreateLoyaltyProgramOutcome.SubscriptionInactive, null);

        if (await loyaltyProgramRepository.GetByStoreIdAsync(command.StoreId, cancellationToken) is not null)
            return new CreateLoyaltyProgramResult(CreateLoyaltyProgramOutcome.AlreadyExists, null);

        var program = new LoyaltyProgram
        {
            StoreId = command.StoreId,
            PointsPerCurrencyUnit = command.PointsPerCurrencyUnit,
            RedemptionRate = command.RedemptionRate,
            IsActive = true
        };

        loyaltyProgramRepository.Add(program);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateLoyaltyProgramResult(CreateLoyaltyProgramOutcome.Created, program.Id);
    }
}
