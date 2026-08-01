using Application.Abstractions;
using Application.Sales.Commands.RecordCommission;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class RecordCommissionCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const string CashierId = "cashier-1";
    private const int StoreId = 1;
    private const int SaleId = 1;

    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<ICommissionRepository> _commissionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RecordCommissionCommandHandler CreateHandler() => new(
        _saleTransactionRepository.Object,
        _storeAccessAuthorizer.Object,
        _commissionRepository.Object,
        _unitOfWork.Object);

    private static SaleTransaction CreateSale() => new()
    {
        StoreId = StoreId,
        CashierUserId = CashierId,
        IdempotencyKey = "key-1",
        Currency = "TJS",
        Status = SaleStatus.Completed,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_SaleNotFound_ReturnsSaleNotFound()
    {
        _saleTransactionRepository.Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync((SaleTransaction?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordCommissionCommand(SaleId, 5, "TJS", OwnerId), CancellationToken.None);

        Assert.Equal(RecordCommissionOutcome.SaleNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotStoreOwner_ReturnsForbidden()
    {
        _saleTransactionRepository.Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateSale());
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordCommissionCommand(SaleId, 5, "TJS", "someone-else"), CancellationToken.None);

        Assert.Equal(RecordCommissionOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_UsesCashierFromSaleNotFromCaller()
    {
        _saleTransactionRepository.Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateSale());
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _commissionRepository.Setup(r => r.Add(It.IsAny<Commission>())).Callback<Commission>(c => c.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new RecordCommissionCommand(SaleId, 5, "TJS", OwnerId), CancellationToken.None);

        Assert.Equal(RecordCommissionOutcome.Recorded, result.Outcome);
        Assert.Equal(1, result.CommissionId);
        _commissionRepository.Verify(r => r.Add(It.Is<Commission>(c => c.CashierUserId == CashierId && c.Amount.Amount == 5)), Times.Once);
    }
}
