using Application.Abstractions;
using Application.Common;
using Domain.Payments;
using Domain.ValueObjects;

namespace Application.Payments.Commands.IssueStoreCredit;

public sealed class IssueStoreCreditCommandHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ICustomerRepository customerRepository,
    IStoreCreditRepository storeCreditRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<IssueStoreCreditCommand, IssueStoreCreditResult>
{
    public async Task<IssueStoreCreditResult> Handle(IssueStoreCreditCommand command, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(command.StoreId, cancellationToken))
            return new IssueStoreCreditResult(IssueStoreCreditOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerAsync(command.StoreId, command.PerformedByUserId, cancellationToken))
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

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new IssueStoreCreditResult(IssueStoreCreditOutcome.Issued, credit.Balance.Amount);
        }

        // A customer's store credit is a single running balance in one currency — merging a
        // different currency's amount into it would silently corrupt the balance (BUG-03).
        if (credit.Balance.Currency != command.Currency)
            return new IssueStoreCreditResult(IssueStoreCreditOutcome.CurrencyMismatch, credit.Balance.Amount);

        await storeCreditRepository.CreditAsync(credit.Id, command.Amount, cancellationToken);

        return new IssueStoreCreditResult(IssueStoreCreditOutcome.Issued, credit.Balance.Amount + command.Amount);
    }
}
