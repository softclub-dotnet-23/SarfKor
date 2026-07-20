using Application.Abstractions;
using Application.Common;
using Domain.Analytics;

namespace Application.Products.Commands.RecordScan;

public sealed class RecordScanCommandHandler(
    IProductRepository productRepository,
    IScanRepository scanRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RecordScanCommand, RecordScanResult>
{
    public async Task<RecordScanResult> Handle(RecordScanCommand command, CancellationToken cancellationToken)
    {
        if (!await productRepository.ExistsAsync(command.ProductId, cancellationToken))
            return new RecordScanResult(RecordScanOutcome.ProductNotFound, null);

        var scan = new Scan
        {
            ProductId = command.ProductId,
            UserId = command.UserId,
            StoreId = command.StoreId,
            ScannedAt = DateTimeOffset.UtcNow
        };

        scanRepository.Add(scan);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RecordScanResult(RecordScanOutcome.Recorded, scan.Id);
    }
}
