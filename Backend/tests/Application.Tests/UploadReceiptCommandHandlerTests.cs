using Application.Abstractions;
using Application.Receipts.Commands.UploadReceipt;
using Domain.Receipts;
using Moq;

namespace Application.Tests;

public class UploadReceiptCommandHandlerTests
{
    private readonly Mock<IReceiptRepository> _receiptRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UploadReceiptCommandHandler CreateHandler() => new(_receiptRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_CreatesPendingReceiptWithLines_AndReturnsItsId()
    {
        Receipt? added = null;
        _receiptRepository.Setup(r => r.Add(It.IsAny<Receipt>())).Callback<Receipt>(r =>
        {
            r.Id = 10;
            added = r;
        });

        var command = new UploadReceiptCommand(
            "user-1",
            StoreId: 2,
            ImageReference: "b3f1e2.jpg",
            Lines:
            [
                new UploadReceiptLineInput(ProductId: 1, RecognizedName: null, Quantity: 2, Price: 12.5m, Currency: "TJS"),
                new UploadReceiptLineInput(ProductId: null, RecognizedName: "Unrecognized item", Quantity: 1, Price: 5m, Currency: "TJS")
            ]);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(10, result.ReceiptId);
        Assert.NotNull(added);
        Assert.Equal("user-1", added!.UserId);
        Assert.Equal(2, added.StoreId);
        Assert.Equal("b3f1e2.jpg", added.ImageReference);
        Assert.Equal(ReceiptVerificationStatus.Pending, added.Status);
        Assert.Equal(2, added.Lines.Count);
        Assert.Equal(1, added.Lines[0].ProductId);
        Assert.Equal(12.5m, added.Lines[0].Price.Amount);
        Assert.Null(added.Lines[1].ProductId);
        Assert.Equal("Unrecognized item", added.Lines[1].RecognizedName);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
