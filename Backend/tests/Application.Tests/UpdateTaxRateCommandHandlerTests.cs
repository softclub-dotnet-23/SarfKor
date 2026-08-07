using Application.Abstractions;
using Application.Catalog.Commands.UpdateTaxRate;
using Domain.Catalog;
using Moq;

namespace Application.Tests;

public class UpdateTaxRateCommandHandlerTests
{
    private readonly Mock<ITaxRateRepository> _taxRateRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateTaxRateCommandHandler CreateHandler() => new(_taxRateRepository.Object, _categoryRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_TaxRateNotFound_ReturnsNotFound()
    {
        _taxRateRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((TaxRate?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateTaxRateCommand(1, "VAT", 15, null, null, null, "admin-1"), CancellationToken.None);

        Assert.Equal(UpdateTaxRateOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsCategoryNotFound()
    {
        _taxRateRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new TaxRate { Name = "VAT", Percentage = 15 });
        _categoryRepository.Setup(r => r.ExistsAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateTaxRateCommand(1, "VAT", 18, 99, null, null, "admin-1"), CancellationToken.None);

        Assert.Equal(UpdateTaxRateOutcome.CategoryNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_Valid_UpdatesFields()
    {
        var taxRate = new TaxRate { Name = "VAT", Percentage = 15, CategoryId = null };
        _taxRateRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(taxRate);
        _categoryRepository.Setup(r => r.ExistsAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateTaxRateCommand(1, "VAT 18", 18, 2, null, null, "admin-1"), CancellationToken.None);

        Assert.Equal(UpdateTaxRateOutcome.Updated, result.Outcome);
        Assert.Equal("VAT 18", taxRate.Name);
        Assert.Equal(18, taxRate.Percentage);
        Assert.Equal(2, taxRate.CategoryId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
