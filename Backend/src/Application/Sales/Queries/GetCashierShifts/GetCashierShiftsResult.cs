namespace Application.Sales.Queries.GetCashierShifts;

public sealed record CashierShiftDto(
    int CashierShiftId,
    string CashierUserId,
    decimal OpeningCash,
    decimal? ExpectedCash,
    decimal? ClosingCash,
    string Currency,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);

public enum GetCashierShiftsOutcome
{
    Found,
    StoreNotFound,
    Forbidden
}

public sealed record GetCashierShiftsResult(GetCashierShiftsOutcome Outcome, IReadOnlyList<CashierShiftDto>? Shifts);
