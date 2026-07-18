using Application.Abstractions;
using Application.Common;

namespace Application.Payments.Queries.GetGiftCardBalance;

public sealed class GetGiftCardBalanceQueryHandler(IGiftCardRepository giftCardRepository) : IQueryHandler<GetGiftCardBalanceQuery, GetGiftCardBalanceResult>
{
    public async Task<GetGiftCardBalanceResult> Handle(GetGiftCardBalanceQuery query, CancellationToken cancellationToken)
    {
        var giftCard = await giftCardRepository.GetByCodeAsync(query.Code, cancellationToken);
        return giftCard is null
            ? new GetGiftCardBalanceResult(false, null, null, null, null)
            : new GetGiftCardBalanceResult(true, giftCard.Balance.Amount, giftCard.Balance.Currency, giftCard.IsActive, giftCard.ExpiresAt);
    }
}
