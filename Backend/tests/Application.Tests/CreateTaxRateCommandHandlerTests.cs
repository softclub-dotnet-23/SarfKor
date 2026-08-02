using Application.Abstractions;
using Application.Catalog.Commands.CreateTaxRate;
using Domain.Catalog;
using Moq;

namespace Application.Tests;

public class CreateTaxRateCommandHandlerTests
{
    private readonly Mock<ITaxRateRepository> _taxRateRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_CreatesTaxRate_AndReturnsItsId()
    {
        _taxRateRepository.Setup(r => r.Add(It.IsAny<TaxRate>())).Callback<TaxRate>(t => t.Id = 1);

        var handler = new CreateTaxRateCommandHandler(_taxRateRepository.Object, _categoryRepository.Object, _unitOfWork.Object);
        var result = await handler.Handle(new CreateTaxRateCommand("VAT", 15, null), CancellationToken.None);

        Assert.Equal(CreateTaxRateOutcome.Created, result.Outcome);
        Assert.Equal(1, result.TaxRateId);
        _taxRateRepository.Verify(r => r.Add(It.Is<TaxRate>(t => t.Name == "VAT" && t.Percentage == 15)), Times.Once);
    }
}
