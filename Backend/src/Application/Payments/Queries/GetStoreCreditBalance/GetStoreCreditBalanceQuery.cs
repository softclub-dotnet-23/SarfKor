namespace Application.Payments.Queries.GetStoreCreditBalance;

public sealed record GetStoreCreditBalanceQuery(int StoreId, int CustomerId, string RequestedByUserId);
