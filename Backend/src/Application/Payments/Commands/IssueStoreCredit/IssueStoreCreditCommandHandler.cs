using Application.Abstractions;
using Application.Common;
using Domain.Payments;
using Domain.ValueObjects;

namespace Application.Payments.Commands.IssueStoreCredit;

public sealed class IssueStoreCreditCommandHandler(
    IStoreRepository storeRepository,
    ICustomerRepository customerRepository,
    IStoreCreditRepository storeCreditRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<IssueStoreCreditCommand, IssueStoreCreditResult>
{
    public async Task<IssueStoreCreditResult> Handle(IssueStoreCreditCommand command, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new IssueStoreCreditResult(IssueStoreCreditOutcome.StoreNotFound, null);

        if (store.OwnerUserId != command.PerformedByUserId)
            return new IssueStoreCreditResult(IssueStoreCreditOutcome.Forbidden, null);

        if (await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken) is null)
            return new IssueStoreCreditResult(IssueStoreCreditOutcome.CustomerNotFound, null);

        var credit = await storeCreditRepository.GetByStoreAndCustomerAsync(command.StoreId, command.CustomerId, cancellationToken);
        if (credit is null)
        {
            credit = new StoreCredit
            {
                StoreId = command.StoreId,
                CustomerId = command.CustomerId,
                Balance = new Money(command.Amount, command.Currency),
                UpdatedAt = DateTimeOffset.UtcNow
            };
            storeCreditRepository.Add(credit);
        }
        else
        {
            credit.Balance = credit.Balance with { Amount = credit.Balance.Amount + command.Amount };
            credit.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssueStoreCreditResult(IssueStoreCreditOutcome.Issued, credit.Balance.Amount);
    }
}
