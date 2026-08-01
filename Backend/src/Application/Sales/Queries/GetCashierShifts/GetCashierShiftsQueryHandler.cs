using Application.Abstractions;
using Application.Common;

namespace Application.Sales.Queries.GetCashierShifts;

public sealed class GetCashierShiftsQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessAuthorizer storeAccessAuthorizer,
    ICashierShiftRepository cashierShiftRepository) : IQueryHandler<GetCashierShiftsQuery, GetCashierShiftsResult>
{
    public async Task<GetCashierShiftsResult> Handle(GetCashierShiftsQuery query, CancellationToken cancellationToken)
    {
        if (!await storeRepository.ExistsAsync(query.StoreId, cancellationToken))
            return new GetCashierShiftsResult(GetCashierShiftsOutcome.StoreNotFound, null);

        if (!await storeAccessAuthorizer.IsOwnerOrEmployeeAsync(query.StoreId, query.RequestedByUserId, cancellationToken))
            return new GetCashierShiftsResult(GetCashierShiftsOutcome.Forbidden, null);

        var shifts = await cashierShiftRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        var dtos = shifts
            .Select(s => new CashierShiftDto(
                s.Id, s.CashierUserId, s.OpeningCash.Amount, s.ExpectedCash?.Amount, s.ClosingCash?.Amount,
                s.OpeningCash.Currency, s.StartedAt, s.EndedAt))
            .ToList();

        return new GetCashierShiftsResult(GetCashierShiftsOutcome.Found, dtos);
    }
}
