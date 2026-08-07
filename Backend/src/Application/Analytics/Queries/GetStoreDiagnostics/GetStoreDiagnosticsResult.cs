using Domain.Stores;
using Domain.Subscriptions;

namespace Application.Analytics.Queries.GetStoreDiagnostics;

public enum GetStoreDiagnosticsOutcome
{
    Found,
    NotFound
}

// ADMIN_PROMPT.md §2.6, verbatim: "Категорически не включать в этот ответ коммерческие данные
// магазина: выручку, себестоимость, наценку, прибыль, суммы конкретных чеков, данные поставщиков и
// закупочные цены." DO NOT add a revenue/profit/margin/receipt-amount/supplier/cost field to this
// record — that is the one boundary this endpoint exists to hold, not an oversight to "complete."
// Everything below is operational health only: presence/timing/counts, never money.
//
// RecentErrors / sync-conflict indicators / client version are intentionally absent, not just
// unpopulated — there is no error-logging, offline-sync, or client-version-reporting
// infrastructure anywhere in this codebase to source them from (CLAUDE.md §10 confirms offline
// mode isn't implemented at all yet); inventing empty placeholder fields for signals that don't
// exist would be indistinguishable from fabricated data to whoever reads this response.
public sealed record GetStoreDiagnosticsResult(
    GetStoreDiagnosticsOutcome Outcome,
    int StoreId,
    StoreStatus? StoreStatus,
    DateTimeOffset? OwnerLastLoginAt,
    DateTimeOffset? LastSaleAt,
    int? StoreLocationsOwnedByThisOwner,
    int? EmployeeCount,
    int? DistinctProductsInStock,
    int? TotalStockUnits,
    SubscriptionStatus? SubscriptionStatus,
    string? SubscriptionPlanName,
    DateTimeOffset? SubscriptionCurrentPeriodEndsAt);
