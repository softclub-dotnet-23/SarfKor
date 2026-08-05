namespace Application.Subscriptions.Queries.GetSubscriptionPayments;

public sealed record GetSubscriptionPaymentsQuery(int Skip, int Take, int? StoreId, DateOnly? From, DateOnly? To);
