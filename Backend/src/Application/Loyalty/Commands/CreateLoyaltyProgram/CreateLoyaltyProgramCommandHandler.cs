using Application.Abstractions;
using Application.Common;
using Domain.Loyalty;

namespace Application.Loyalty.Commands.CreateLoyaltyProgram;

public sealed class CreateLoyaltyProgramCommandHandler(
    IStoreRepository storeRepository,
    ILoyaltyProgramRepository loyaltyProgramRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateLoyaltyProgramCommand, CreateLoyaltyProgramResult>
{
    public async Task<CreateLoyaltyProgramResult> Handle(CreateLoyaltyProgramCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new CreateLoyaltyProgramResult(CreateLoyaltyProgramOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId)
            return new CreateLoyaltyProgramResult(CreateLoyaltyProgramOutcome.Forbidden, null);

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
