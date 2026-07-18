using Application.Abstractions;
using Application.Common;
using Domain.Receipts;
using Domain.ValueObjects;

namespace Application.Receipts.Commands.UploadReceipt;

public sealed class UploadReceiptCommandHandler(
    IReceiptRepository receiptRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UploadReceiptCommand, UploadReceiptResult>
{
    public async Task<UploadReceiptResult> Handle(UploadReceiptCommand command, CancellationToken cancellationToken)
    {
        var receipt = new Receipt
        {
            UserId = command.UserId,
            StoreId = command.StoreId,
            ImageReference = command.ImageReference,
            Status = ReceiptVerificationStatus.Pending,
            UploadedAt = DateTimeOffset.UtcNow,
            Lines = command.Lines
                .Select(l => new ReceiptLineItem
                {
                    ProductId = l.ProductId,
                    RecognizedName = l.RecognizedName,
                    Quantity = l.Quantity,
                    Price = new Money(l.Price, l.Currency)
                })
                .ToList()
        };

        receiptRepository.Add(receipt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UploadReceiptResult(receipt.Id);
    }
}
