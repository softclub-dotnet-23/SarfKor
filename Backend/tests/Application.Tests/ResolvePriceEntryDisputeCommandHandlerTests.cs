using Application.Abstractions;
using Application.Pricing.Commands.ResolvePriceEntryDispute;
using Domain.Auditing;
using Domain.Pricing;
using Moq;

namespace Application.Tests;

public class ResolvePriceEntryDisputeCommandHandlerTests
{
    private const int DisputeId = 1;
    private const string AdminUserId = "admin-1";

    private readonly Mock<IPriceEntryDisputeRepository> _priceEntryDisputeRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ResolvePriceEntryDisputeCommandHandler CreateHandler() => new(_priceEntryDisputeRepository.Object, _auditLogRepository.Object, _unitOfWork.Object);

    private static PriceEntryDispute CreateDispute(PriceEntryDisputeStatus status) => new()
    {
        PriceEntryId = 1,
        DisputedByUserId = "user-1",
        Reason = "Wrong price",
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_DisputeNotFound_ReturnsNotFound()
    {
        _priceEntryDisputeRepository.Setup(r => r.GetByIdAsync(DisputeId, It.IsAny<CancellationToken>())).ReturnsAsync((PriceEntryDispute?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolvePriceEntryDisputeCommand(DisputeId, true, AdminUserId), CancellationToken.None);

        Assert.Equal(ResolvePriceEntryDisputeOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_AlreadyResolved_ReturnsAlreadyResolved()
    {
        _priceEntryDisputeRepository
            .Setup(r => r.GetByIdAsync(DisputeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDispute(PriceEntryDisputeStatus.Upheld));

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolvePriceEntryDisputeCommand(DisputeId, true, AdminUserId), CancellationToken.None);

        Assert.Equal(ResolvePriceEntryDisputeOutcome.AlreadyResolved, result.Outcome);
        _auditLogRepository.Verify(r => r.Add(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Uphold_SetsUpheldAndWritesAuditLog()
    {
        var dispute = CreateDispute(PriceEntryDisputeStatus.Pending);
        _priceEntryDisputeRepository.Setup(r => r.GetByIdAsync(DisputeId, It.IsAny<CancellationToken>())).ReturnsAsync(dispute);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolvePriceEntryDisputeCommand(DisputeId, true, AdminUserId), CancellationToken.None);

        Assert.Equal(ResolvePriceEntryDisputeOutcome.Upheld, result.Outcome);
        Assert.Equal(PriceEntryDisputeStatus.Upheld, dispute.Status);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "PriceEntryDispute.Upheld")), Times.Once);
    }

    [Fact]
    public async Task Handle_Dismiss_SetsDismissedAndWritesAuditLog()
    {
        var dispute = CreateDispute(PriceEntryDisputeStatus.Pending);
        _priceEntryDisputeRepository.Setup(r => r.GetByIdAsync(DisputeId, It.IsAny<CancellationToken>())).ReturnsAsync(dispute);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolvePriceEntryDisputeCommand(DisputeId, false, AdminUserId), CancellationToken.None);

        Assert.Equal(ResolvePriceEntryDisputeOutcome.Dismissed, result.Outcome);
        Assert.Equal(PriceEntryDisputeStatus.Dismissed, dispute.Status);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.Action == "PriceEntryDispute.Dismissed")), Times.Once);
    }
}
