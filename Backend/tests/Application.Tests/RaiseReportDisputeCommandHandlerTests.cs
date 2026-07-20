using Application.Abstractions;
using Application.Feedback.Commands.RaiseReportDispute;
using Domain.Feedback;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class RaiseReportDisputeCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int ReportId = 1;
    private const int StoreId = 1;

    private readonly Mock<IReportRepository> _reportRepository = new();
    private readonly Mock<IStoreRepository> _storeRepository = new();
    private readonly Mock<IReportDisputeRepository> _reportDisputeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RaiseReportDisputeCommandHandler CreateHandler() => new(_reportRepository.Object, _storeRepository.Object, _reportDisputeRepository.Object, _unitOfWork.Object);

    private static Report CreateReport(int? storeId) => new()
    {
        UserId = "user-1",
        ProductId = 1,
        StoreId = storeId,
        Type = ReportType.WrongPrice,
        Description = "test",
        Status = ReportStatus.Pending,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_ReportNotFound_ReturnsReportNotFound()
    {
        _reportRepository.Setup(r => r.GetByIdAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync((Report?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RaiseReportDisputeCommand(ReportId, OwnerId, "Not accurate"), CancellationToken.None);

        Assert.Equal(RaiseReportDisputeOutcome.ReportNotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_ReportHasNoStore_ReturnsForbidden()
    {
        _reportRepository.Setup(r => r.GetByIdAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateReport(storeId: null));

        var handler = CreateHandler();
        var result = await handler.Handle(new RaiseReportDisputeCommand(ReportId, OwnerId, "Not accurate"), CancellationToken.None);

        Assert.Equal(RaiseReportDisputeOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotStoreOwner_ReturnsForbidden()
    {
        _reportRepository.Setup(r => r.GetByIdAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateReport(StoreId));
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });

        var handler = CreateHandler();
        var result = await handler.Handle(new RaiseReportDisputeCommand(ReportId, "someone-else", "Not accurate"), CancellationToken.None);

        Assert.Equal(RaiseReportDisputeOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_StoreOwnerDisputesReportAboutTheirStore_CreatesPendingDispute()
    {
        _reportRepository.Setup(r => r.GetByIdAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateReport(StoreId));
        _storeRepository
            .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Store { OwnerUserId = OwnerId, Name = "Test", Address = "Addr", Location = new GeoLocation(0, 0) });
        _reportDisputeRepository.Setup(r => r.Add(It.IsAny<ReportDispute>())).Callback<ReportDispute>(d => d.Id = 1);

        var handler = CreateHandler();
        var result = await handler.Handle(new RaiseReportDisputeCommand(ReportId, OwnerId, "Not accurate"), CancellationToken.None);

        Assert.Equal(RaiseReportDisputeOutcome.Raised, result.Outcome);
        Assert.Equal(1, result.DisputeId);
        _reportDisputeRepository.Verify(r => r.Add(It.Is<ReportDispute>(d => d.Status == ReportDisputeStatus.Pending)), Times.Once);
    }
}
