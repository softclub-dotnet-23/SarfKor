using Application.Abstractions;
using Application.Common;
using Domain.Receipts;

namespace Application.Receipts.Commands.VerifyReceipt;

public sealed class VerifyReceiptCommandHandler(
    IReceiptRepository receiptRepository,
    IPriceEntryRepository priceEntryRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<VerifyReceiptCommand, VerifyReceiptResult>
{
    public async Task<VerifyReceiptResult> Handle(VerifyReceiptCommand command, CancellationToken cancellationToken)
    {
        var receipt = await receiptRepository.GetByIdAsync(command.ReceiptId, cancellationToken);
        if (receipt is null)
            return new VerifyReceiptResult(VerifyReceiptOutcome.NotFound, null);

        if (receipt.UserId != command.RequestedByUserId)
            return new VerifyReceiptResult(VerifyReceiptOutcome.Forbidden, null);

        if (receipt.Status != ReceiptVerificationStatus.Pending)
            return new VerifyReceiptResult(VerifyReceiptOutcome.AlreadyProcessed, null);

        if (receipt.StoreId is null)
            return new VerifyReceiptResult(VerifyReceiptOutcome.MissingStore, null);

        var comparisons = new List<ReceiptLineComparisonDto>();

        foreach (var line in receipt.Lines)
        {
            if (line.ProductId is null)
            {
                comparisons.Add(new ReceiptLineComparisonDto(null, line.Price.Amount, null, false));
                continue;
            }

            var currentPrice = await priceEntryRepository.GetLatestForStoreAsync(line.ProductId.Value, receipt.StoreId.Value, cancellationToken);
            var matches = currentPrice is not null && currentPrice.Price.Amount == line.Price.Amount;

            comparisons.Add(new ReceiptLineComparisonDto(line.ProductId, line.Price.Amount, currentPrice?.Price.Amount, matches));
        }

        var allMatch = comparisons.Count > 0 && comparisons.All(c => c.Matches);

        receipt.Status = allMatch ? ReceiptVerificationStatus.Verified : ReceiptVerificationStatus.Mismatched;
        receipt.VerifiedAt = DateTimeOffset.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new VerifyReceiptResult(
            allMatch ? VerifyReceiptOutcome.Verified : VerifyReceiptOutcome.Mismatched,
            comparisons);
    }
}
