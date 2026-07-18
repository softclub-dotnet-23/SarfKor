using Application.Abstractions;
using Application.Common;
using Domain.Catalog;

namespace Application.Catalog.Commands.CreateBrand;

public sealed class CreateBrandCommandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateBrandCommand, CreateBrandResult>
{
    public async Task<CreateBrandResult> Handle(CreateBrandCommand command, CancellationToken cancellationToken)
    {
        var brand = new Brand { Name = command.Name };
        brandRepository.Add(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateBrandResult(brand.Id);
    }
}
