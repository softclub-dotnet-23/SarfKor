using Application.Abstractions;
using Application.Catalog.Commands.DeleteTaxRate;
using Domain.Catalog;
using Moq;

namespace Application.Tests;

public class DeleteTaxRateCommandHandlerTests
{
    private readonly Mock<ITaxRateRepository> _taxRateRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteTaxRateCommandHandler CreateHandler() => new(_taxRateRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_TaxRateNotFound_ReturnsNotFound()
    {
        _taxRateRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((TaxRate?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteTaxRateCommand(1, "admin-1"), CancellationToken.None);

        Assert.Equal(DeleteTaxRateOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_TaxRateInUse_ReturnsInUseAndDoesNotDelete()
    {
        var taxRate = new TaxRate { Name = "VAT", Percentage = 15 };
        _taxRateRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(taxRate);
        _taxRateRepository.Setup(r => r.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteTaxRateCommand(1, "admin-1"), CancellationToken.None);

        Assert.Equal(DeleteTaxRateOutcome.InUse, result.Outcome);
        _taxRateRepository.Verify(r => r.Remove(It.IsAny<TaxRate>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotInUse_Deletes()
    {
        var taxRate = new TaxRate { Name = "VAT", Percentage = 15 };
        _taxRateRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(taxRate);
        _taxRateRepository.Setup(r => r.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteTaxRateCommand(1, "admin-1"), CancellationToken.None);

        Assert.Equal(DeleteTaxRateOutcome.Deleted, result.Outcome);
        _taxRateRepository.Verify(r => r.Remove(taxRate), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
