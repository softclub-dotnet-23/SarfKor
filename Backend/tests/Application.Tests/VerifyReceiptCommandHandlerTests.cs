using Application.Abstractions;
using Application.Receipts.Commands.VerifyReceipt;
using Domain.Pricing;
using Domain.Receipts;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class VerifyReceiptCommandHandlerTests
{
    private const int ReceiptId = 1;
    private const string OwnerId = "user-1";
    private const int StoreId = 2;

    private readonly Mock<IReceiptRepository> _receiptRepository = new();
    private readonly Mock<IPriceEntryRepository> _priceEntryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private VerifyReceiptCommandHandler CreateHandler() => new(
        _receiptRepository.Object,
        _priceEntryRepository.Object,
        _unitOfWork.Object);

    private static Receipt CreateReceipt(
        ReceiptVerificationStatus status = ReceiptVerificationStatus.Pending,
        int? storeId = StoreId,
        List<ReceiptLineItem>? lines = null) => new()
    {
        UserId = OwnerId,
        StoreId = storeId,
        ImageReference = "receipt.jpg",
        Status = status,
        UploadedAt = DateTimeOffset.UtcNow,
        Lines = lines ?? [new ReceiptLineItem { ProductId = 1, Quantity = 1, Price = new Money(10, "TJS") }]
    };

    [Fact]
    public async Task Handle_ReceiptNotFound_ReturnsNotFound()
    {
        _receiptRepository.Setup(r => r.GetByIdAsync(ReceiptId, It.IsAny<CancellationToken>())).ReturnsAsync((Receipt?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyReceiptCommand(ReceiptId, OwnerId), CancellationToken.None);

        Assert.Equal(VerifyReceiptOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotUploader_ReturnsForbidden()
    {
        _receiptRepository.Setup(r => r.GetByIdAsync(ReceiptId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateReceipt());

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyReceiptCommand(ReceiptId, "someone-else"), CancellationToken.None);

        Assert.Equal(VerifyReceiptOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyProcessed_ReturnsAlreadyProcessed()
    {
        _receiptRepository
            .Setup(r => r.GetByIdAsync(ReceiptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateReceipt(ReceiptVerificationStatus.Verified));

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyReceiptCommand(ReceiptId, OwnerId), CancellationToken.None);

        Assert.Equal(VerifyReceiptOutcome.AlreadyProcessed, result.Outcome);
    }

    [Fact]
    public async Task Handle_NoStoreOnReceipt_ReturnsMissingStore()
    {
        _receiptRepository
            .Setup(r => r.GetByIdAsync(ReceiptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateReceipt(storeId: null));

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyReceiptCommand(ReceiptId, OwnerId), CancellationToken.None);

        Assert.Equal(VerifyReceiptOutcome.MissingStore, result.Outcome);
    }

    [Fact]
    public async Task Handle_AllLinesMatchCurrentPrice_ReturnsVerified()
    {
        var receipt = CreateReceipt(lines: [new ReceiptLineItem { ProductId = 1, Quantity = 1, Price = new Money(10, "TJS") }]);
        _receiptRepository.Setup(r => r.GetByIdAsync(ReceiptId, It.IsAny<CancellationToken>())).ReturnsAsync(receipt);
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreAsync(1, StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEntry { ProductId = 1, StoreId = StoreId, Price = new Money(10, "TJS") });

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyReceiptCommand(ReceiptId, OwnerId), CancellationToken.None);

        Assert.Equal(VerifyReceiptOutcome.Verified, result.Outcome);
        Assert.Equal(ReceiptVerificationStatus.Verified, receipt.Status);
        Assert.True(result.Lines![0].Matches);
    }

    [Fact]
    public async Task Handle_PriceMismatch_ReturnsMismatchedAndDoesNotThrow()
    {
        var receipt = CreateReceipt(lines: [new ReceiptLineItem { ProductId = 1, Quantity = 1, Price = new Money(10, "TJS") }]);
        _receiptRepository.Setup(r => r.GetByIdAsync(ReceiptId, It.IsAny<CancellationToken>())).ReturnsAsync(receipt);
        _priceEntryRepository
            .Setup(r => r.GetLatestForStoreAsync(1, StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEntry { ProductId = 1, StoreId = StoreId, Price = new Money(15, "TJS") });

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyReceiptCommand(ReceiptId, OwnerId), CancellationToken.None);

        Assert.Equal(VerifyReceiptOutcome.Mismatched, result.Outcome);
        Assert.Equal(ReceiptVerificationStatus.Mismatched, receipt.Status);
        Assert.False(result.Lines![0].Matches);
    }
}
