using Application.Abstractions;
using Application.Sales.Commands.OpenCashierShift;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class OpenCashierShiftCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;

    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<ICashierShiftRepository> _cashierShiftRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private OpenCashierShiftCommandHandler CreateHandler() => new(_storeRepository.Object, _cashierShiftRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_StoreNotFound_ReturnsStoreNotFound()
    {
        _storeRepository.Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>())).ReturnsAsync((Store?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new OpenCashierShiftCommand(StoreId, 100, "TJS", OwnerId), CancellationToken.None);

        Assert.Equal(OpenCashierShiftOutcome.StoreNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new OpenCashierShiftCommand(StoreId, 100, "TJS", "someone-else"), CancellationToken.None);

        Assert.Equal(OpenCashierShiftOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_OpensShiftWithOpeningCash()
    {
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
        CashierShift? added = null;
        _cashierShiftRepository.Setup(r => r.Add(It.IsAny<CashierShift>())).Callback<CashierShift>(s =>
        {
            s.Id = 1;
            added = s;
        });

        var handler = CreateHandler();
        var result = await handler.Handle(new OpenCashierShiftCommand(StoreId, 100, "TJS", OwnerId), CancellationToken.None);

        Assert.Equal(OpenCashierShiftOutcome.Opened, result.Outcome);
        Assert.Equal(1, result.CashierShiftId);
        Assert.Equal(100, added!.OpeningCash.Amount);
        Assert.Null(added.EndedAt);
    }
}
