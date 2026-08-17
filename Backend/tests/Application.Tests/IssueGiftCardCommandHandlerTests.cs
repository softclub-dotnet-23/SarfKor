using Application.Abstractions;
using Application.Payments.Commands.IssueGiftCard;
using Domain.Payments;
using Moq;

namespace Application.Tests;

public class IssueGiftCardCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IGiftCardRepository> _giftCardRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private IssueGiftCardCommandHandler CreateHandler() =>
        new(_storeRepository.Object, _storeAccessAuthorizer.Object, _giftCardRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new IssueGiftCardCommand(StoreId, OwnerId, 50, "TJS", null), CancellationToken.None);

        Assert.Equal(IssueGiftCardOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwnerOrEmployee_ReturnsForbidden()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, "someone-else", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new IssueGiftCardCommand(StoreId, "someone-else", 50, "TJS", null), CancellationToken.None);

        Assert.Equal(IssueGiftCardOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_IssuesActiveCardWithGeneratedCode()
    {
        _storeRepository.Setup(r => r.ExistsAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _giftCardRepository.Setup(r => r.Add(It.IsAny<GiftCard>())).Callback<GiftCard>(g => g.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new IssueGiftCardCommand(StoreId, OwnerId, 50, "TJS", null), CancellationToken.None);

        Assert.Equal(IssueGiftCardOutcome.Issued, result.Outcome);
        Assert.Equal(1, result.GiftCardId);
        Assert.False(string.IsNullOrWhiteSpace(result.Code));
        _giftCardRepository.Verify(r => r.Add(It.Is<GiftCard>(g => g.IsActive && g.Balance.Amount == 50 && g.IssuingStoreId == StoreId)), Times.Once);
    }
}
