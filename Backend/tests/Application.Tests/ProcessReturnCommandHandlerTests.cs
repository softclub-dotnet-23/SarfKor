using Application.Abstractions;
using Application.Sales.Commands.ProcessReturn;
using Domain.Sales;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class ProcessReturnCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int SaleId = 1;
    private const int LineId = 10;

    private readonly Mock<ISaleTransactionRepository> _saleTransactionRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<ISaleReturnRepository> _saleReturnRepository = new();
    private readonly Mock<IStockLevelRepository> _stockLevelRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovementRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public ProcessReturnCommandHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        _storeAccessAuthorizer
            .Setup(a => a.IsOwnerOrEmployeeAsync(StoreId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private ProcessReturnCommandHandler CreateHandler() => new(
        _saleTransactionRepository.Object,
        _storeAccessAuthorizer.Object,
        _saleReturnRepository.Object,
        _stockLevelRepository.Object,
        _stockMovementRepository.Object,
        _unitOfWork.Object);

    private static SaleTransaction CreateSale(SaleStatus status = SaleStatus.Completed, int quantity = 5)
    {
        var sale = new SaleTransaction
        {
            StoreId = StoreId,
            CashierUserId = OwnerId,
            IdempotencyKey = "key-1",
            Currency = "TJS",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = [new SaleLineItem { ProductId = 1, Quantity = quantity, UnitPriceAtSale = new Money(10, "TJS") }]
        };
        sale.Lines[0].Id = LineId;
        return sale;
    }

    [Fact]
    public async Task Handle_SaleNotFound_ReturnsSaleNotFound()
    {
        _saleTransactionRepository.Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync((SaleTransaction?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessReturnCommand(SaleId, [new ProcessReturnLineInput(LineId, 1)], "Damaged", OwnerId), CancellationToken.None);

        Assert.Equal(ProcessReturnOutcome.SaleNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_SaleNotCompleted_ReturnsSaleNotCompleted()
    {
        _saleTransactionRepository.Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateSale(SaleStatus.Voided));

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessReturnCommand(SaleId, [new ProcessReturnLineInput(LineId, 1)], "Damaged", OwnerId), CancellationToken.None);

        Assert.Equal(ProcessReturnOutcome.SaleNotCompleted, result.Outcome);
    }

    [Fact]
    public async Task Handle_LineNotOnSale_ReturnsLineNotFound()
    {
        _saleTransactionRepository.Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateSale());
        _saleReturnRepository.Setup(r => r.GetBySaleTransactionIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessReturnCommand(SaleId, [new ProcessReturnLineInput(999, 1)], "Damaged", OwnerId), CancellationToken.None);

        Assert.Equal(ProcessReturnOutcome.LineNotFound, result.Outcome);
        Assert.Equal(999, result.FailedSaleLineItemId);
    }

    [Fact]
    public async Task Handle_QuantityExceedsOriginal_ReturnsExceedsAvailableQuantity()
    {
        _saleTransactionRepository.Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateSale(quantity: 5));
        _saleReturnRepository.Setup(r => r.GetBySaleTransactionIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessReturnCommand(SaleId, [new ProcessReturnLineInput(LineId, 6)], "Damaged", OwnerId), CancellationToken.None);

        Assert.Equal(ProcessReturnOutcome.ExceedsAvailableQuantity, result.Outcome);
    }

    [Fact]
    public async Task Handle_QuantityExceedsRemainingAfterPriorPartialReturn_ReturnsExceedsAvailableQuantity()
    {
        // Sale had 5 units; 3 already returned in a prior SaleReturn. Only 2 remain — requesting 3 must fail.
        _saleTransactionRepository.Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateSale(quantity: 5));
        var priorReturn = new SaleReturn
        {
            SaleTransactionId = SaleId,
            ProcessedByUserId = OwnerId,
            Reason = "prior",
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = [new ReturnLineItem { SaleLineItemId = LineId, Quantity = 3, RefundAmount = new Money(30, "TJS") }]
        };
        _saleReturnRepository.Setup(r => r.GetBySaleTransactionIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync([priorReturn]);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessReturnCommand(SaleId, [new ProcessReturnLineInput(LineId, 3)], "Damaged", OwnerId), CancellationToken.None);

        Assert.Equal(ProcessReturnOutcome.ExceedsAvailableQuantity, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidReturn_RestoresStockAndComputesRefund()
    {
        _saleTransactionRepository.Setup(r => r.GetByIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateSale(quantity: 5));
        _saleReturnRepository.Setup(r => r.GetBySaleTransactionIdAsync(SaleId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _saleReturnRepository.Setup(r => r.Add(It.IsAny<SaleReturn>())).Callback<SaleReturn>(sr => sr.Id = 5);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessReturnCommand(SaleId, [new ProcessReturnLineInput(LineId, 2)], "Damaged", OwnerId), CancellationToken.None);

        Assert.Equal(ProcessReturnOutcome.Processed, result.Outcome);
        Assert.Equal(5, result.SaleReturnId);
        Assert.Equal(20, result.TotalRefund);
        _stockLevelRepository.Verify(r => r.IncrementAsync(1, StoreId, 2, It.IsAny<CancellationToken>()), Times.Once);
        _stockMovementRepository.Verify(r => r.Add(It.IsAny<Domain.Inventory.StockMovement>()), Times.Once);
    }
}
