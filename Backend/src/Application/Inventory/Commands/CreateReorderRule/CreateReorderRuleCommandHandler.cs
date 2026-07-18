using Application.Abstractions;
using Application.Common;
using Domain.Inventory;

namespace Application.Inventory.Commands.CreateReorderRule;

public sealed class CreateReorderRuleCommandHandler(
    IStoreRepository storeRepository,
    IReorderRuleRepository reorderRuleRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateReorderRuleCommand, CreateReorderRuleResult>
{
    public async Task<CreateReorderRuleResult> Handle(CreateReorderRuleCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new CreateReorderRuleResult(CreateReorderRuleOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId)
            return new CreateReorderRuleResult(CreateReorderRuleOutcome.Forbidden, null);

        var rule = new ReorderRule
        {
            StoreId = command.StoreId,
            ProductId = command.ProductId,
            ThresholdQuantity = command.ThresholdQuantity,
            ReorderQuantity = command.ReorderQuantity,
            PreferredSupplierId = command.PreferredSupplierId,
            IsActive = true
        };

        reorderRuleRepository.Add(rule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateReorderRuleResult(CreateReorderRuleOutcome.Created, rule.Id);
    }
}
