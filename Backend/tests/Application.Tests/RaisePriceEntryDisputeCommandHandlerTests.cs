using Application.Abstractions;
using Application.Pricing.Commands.RaisePriceEntryDispute;
using Domain.Pricing;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class RaisePriceEntryDisputeCommandHandlerTests
{
    private readonly Mock<IPriceEntryRepository> _priceEntryRepository = new();
    private readonly Mock<IPriceEntryDisputeRepository> _priceEntryDisputeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RaisePriceEntryDisputeCommandHandler CreateHandler() => new(_priceEntryRepository.Object, _priceEntryDisputeRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_PriceEntryNotFound_ReturnsPriceEntryNotFound()
    {
        _priceEntryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((PriceEntry?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RaisePriceEntryDisputeCommand(1, "user-1", "Wrong price"), CancellationToken.None);

        Assert.Equal(RaisePriceEntryDisputeOutcome.PriceEntryNotFound, result.Outcome);
        _priceEntryDisputeRepository.Verify(r => r.Add(It.IsAny<PriceEntryDispute>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPendingDispute()
    {
        _priceEntryRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceEntry { ProductId = 1, StoreId = 1, Price = new Money(10, "TJS") });
        _priceEntryDisputeRepository.Setup(r => r.Add(It.IsAny<PriceEntryDispute>())).Callback<PriceEntryDispute>(d => d.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new RaisePriceEntryDisputeCommand(1, "user-1", "Wrong price"), CancellationToken.None);

        Assert.Equal(RaisePriceEntryDisputeOutcome.Raised, result.Outcome);
        Assert.Equal(1, result.DisputeId);
        _priceEntryDisputeRepository.Verify(r => r.Add(It.Is<PriceEntryDispute>(d => d.Status == PriceEntryDisputeStatus.Pending)), Times.Once);
    }
}
