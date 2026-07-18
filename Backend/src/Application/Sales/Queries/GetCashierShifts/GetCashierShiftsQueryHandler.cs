using Application.Abstractions;
using Application.Common;

namespace Application.Sales.Queries.GetCashierShifts;

public sealed class GetCashierShiftsQueryHandler(
    IStoreRepository storeRepository,
    ICashierShiftRepository cashierShiftRepository) : IQueryHandler<GetCashierShiftsQuery, GetCashierShiftsResult>
{
    public async Task<GetCashierShiftsResult> Handle(GetCashierShiftsQuery query, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
            return new GetCashierShiftsResult(GetCashierShiftsOutcome.StoreNotFound, null);

        if (store.OwnerUserId != query.RequestedByUserId)
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
