using Application.Abstractions;
using Application.Common;

namespace Application.Pricing.Queries.GetPendingPriceEntryDisputes;

public sealed class GetPendingPriceEntryDisputesQueryHandler(IPriceEntryDisputeRepository priceEntryDisputeRepository)
    : IQueryHandler<GetPendingPriceEntryDisputesQuery, GetPendingPriceEntryDisputesResult>
{
    public async Task<GetPendingPriceEntryDisputesResult> Handle(GetPendingPriceEntryDisputesQuery query, CancellationToken cancellationToken)
    {
        var disputes = await priceEntryDisputeRepository.GetPendingAsync(cancellationToken);
        var dtos = disputes.Select(d => new PriceEntryDisputeDto(d.Id, d.PriceEntryId, d.DisputedByUserId, d.Reason, d.CreatedAt)).ToList();
        return new GetPendingPriceEntryDisputesResult(dtos);
    }
}
