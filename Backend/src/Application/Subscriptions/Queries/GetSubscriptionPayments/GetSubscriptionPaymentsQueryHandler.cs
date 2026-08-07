using Application.Abstractions;
using Application.Common;

namespace Application.Subscriptions.Queries.GetSubscriptionPayments;

public sealed class GetSubscriptionPaymentsQueryHandler(
    ISubscriptionPaymentRepository subscriptionPaymentRepository,
    IStoreSubscriptionRepository storeSubscriptionRepository,
    IStoreRepository storeRepository,
    IAuthService authService) : IQueryHandler<GetSubscriptionPaymentsQuery, GetSubscriptionPaymentsResult>
{
    public async Task<GetSubscriptionPaymentsResult> Handle(GetSubscriptionPaymentsQuery query, CancellationToken cancellationToken)
    {
        var payments = await subscriptionPaymentRepository.GetAllAsync(query.Skip, query.Take, query.StoreId, query.From, query.To, cancellationToken);
        var totalCount = await subscriptionPaymentRepository.CountAllAsync(query.StoreId, query.From, query.To, cancellationToken);

        var subscriptionIds = payments.Select(p => p.StoreSubscriptionId).Distinct().ToList();
        var subscriptionsById = new Dictionary<int, int>(); // subscriptionId -> storeId
        foreach (var id in subscriptionIds)
        {
            var sub = await storeSubscriptionRepository.GetByIdAsync(id, cancellationToken);
            if (sub is not null) subscriptionsById[id] = sub.StoreId;
        }

        var storesById = (await storeRepository.GetByIdsAsync(subscriptionsById.Values.Distinct().ToList(), cancellationToken))
            .ToDictionary(s => s.Id);

        var recordedByIds = payments.Where(p => p.RecordedByUserId is not null).Select(p => p.RecordedByUserId!).Distinct().ToList();
        var emailsByUserId = await authService.GetEmailsByUserIdsAsync(recordedByIds, cancellationToken);

        var dtos = payments.Select(p =>
        {
            var storeId = subscriptionsById.GetValueOrDefault(p.StoreSubscriptionId);
            var storeName = storesById.GetValueOrDefault(storeId)?.Name ?? $"Store #{storeId}";
            return new SubscriptionPaymentDto(
                p.Id,
                p.StoreSubscriptionId,
                storeId,
                storeName,
                p.Amount.Amount,
                p.Amount.Currency,
                p.PeriodStart,
                p.PeriodEnd,
                p.Method,
                p.Comment,
                p.RecordedByUserId,
                p.RecordedByUserId is not null ? emailsByUserId.GetValueOrDefault(p.RecordedByUserId) : null,
                p.RecordedAt,
                p.IsReversal,
                p.ReversedPaymentId);
        }).ToList();

        return new GetSubscriptionPaymentsResult(dtos, totalCount);
    }
}
