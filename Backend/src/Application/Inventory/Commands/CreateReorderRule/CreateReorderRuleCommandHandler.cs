using Application.Abstractions;
using Application.Common;
using Domain.Inventory;

namespace Application.Inventory.Commands.CreateReorderRule;

public sealed class CreateReorderRuleCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IProductRepository productRepository,
    ISupplierRepository supplierRepository,
    IReorderRuleRepository reorderRuleRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateReorderRuleCommand, CreateReorderRuleResult>
{
    public async Task<CreateReorderRuleResult> Handle(CreateReorderRuleCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new CreateReorderRuleResult(CreateReorderRuleOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
            return new CreateReorderRuleResult(CreateReorderRuleOutcome.Forbidden, null);

        if (!await storeAccessAuthorizer.IsOperationalAsync(command.StoreId, cancellationToken))
            return new CreateReorderRuleResult(CreateReorderRuleOutcome.SubscriptionInactive, null);

        if (!await productRepository.ExistsAsync(command.ProductId, cancellationToken))
            return new CreateReorderRuleResult(CreateReorderRuleOutcome.ProductNotFound, null);

        if (command.PreferredSupplierId.HasValue
            && await supplierRepository.GetByIdAsync(command.PreferredSupplierId.Value, cancellationToken) is null)
            return new CreateReorderRuleResult(CreateReorderRuleOutcome.SupplierNotFound, null);

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
