using Application.Abstractions;
using Application.Common;
using Domain.Loyalty;

namespace Application.Loyalty.Commands.EnrollCustomerInLoyalty;

public sealed class EnrollCustomerInLoyaltyCommandHandler(
    ICustomerRepository customerRepository,
    ILoyaltyProgramRepository loyaltyProgramRepository,
    ILoyaltyAccountRepository loyaltyAccountRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    IUnitOfWork unitOfWork) : ICommandHandler<EnrollCustomerInLoyaltyCommand, EnrollCustomerInLoyaltyResult>
{
    public async Task<EnrollCustomerInLoyaltyResult> Handle(EnrollCustomerInLoyaltyCommand command, CancellationToken cancellationToken)
    {
        if (await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken) is null)
            return new EnrollCustomerInLoyaltyResult(EnrollCustomerInLoyaltyOutcome.CustomerNotFound, null);

        var program = await loyaltyProgramRepository.GetByIdAsync(command.LoyaltyProgramId, cancellationToken);
        if (program is null)
            return new EnrollCustomerInLoyaltyResult(EnrollCustomerInLoyaltyOutcome.ProgramNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(program.StoreId, command.PerformedByUserId, cancellationToken))
            return new EnrollCustomerInLoyaltyResult(EnrollCustomerInLoyaltyOutcome.Forbidden, null);

        var existing = await loyaltyAccountRepository.GetByCustomerAndProgramAsync(command.CustomerId, command.LoyaltyProgramId, cancellationToken);
        if (existing is not null)
            return new EnrollCustomerInLoyaltyResult(EnrollCustomerInLoyaltyOutcome.AlreadyEnrolled, existing.Id);

        var account = new LoyaltyAccount { CustomerId = command.CustomerId, LoyaltyProgramId = command.LoyaltyProgramId, PointsBalance = 0 };
        loyaltyAccountRepository.Add(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new EnrollCustomerInLoyaltyResult(EnrollCustomerInLoyaltyOutcome.Enrolled, account.Id);
    }
}
