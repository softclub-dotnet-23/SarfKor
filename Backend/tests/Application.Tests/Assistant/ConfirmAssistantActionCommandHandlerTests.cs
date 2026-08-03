using Application.Abstractions;
using Application.Assistant;
using Application.Assistant.Abstractions;
using Application.Assistant.Commands.ConfirmAssistantAction;
using Domain.Assistant;
using Domain.Auditing;
using Microsoft.Extensions.Options;
using Moq;

namespace Application.Tests.Assistant;

public class ConfirmAssistantActionCommandHandlerTests
{
    private readonly Mock<IPendingAssistantActionRepository> _pendingActionRepository = new();
    private readonly Mock<IPendingActionExecutor> _executor = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ConfirmAssistantActionCommandHandler CreateHandler(bool actionsEnabled = true) => new(
        _pendingActionRepository.Object,
        [_executor.Object],
        _auditLogRepository.Object,
        _unitOfWork.Object,
        Options.Create(new AssistantOptions { ActionsEnabled = actionsEnabled }));

    private static PendingAssistantAction NewPendingAction(DateTimeOffset? expiresAt = null, DateTimeOffset? confirmedAt = null) => new()
    {
        Id = 1,
        RequestedByUserId = "user-1",
        StoreId = 10,
        ActionType = AssistantActionType.SetPrice,
        ParametersJson = """{"productId":1,"price":5,"currency":"TJS"}""",
        Summary = "Установить цену «Хлеб» на 5 TJS",
        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(15),
        ConfirmedAt = confirmedAt,
    };

    [Fact]
    public async Task Handle_PendingActionNotFound_ReturnsNotFound()
    {
        _pendingActionRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((PendingAssistantAction?)null);

        var result = await CreateHandler().Handle(new ConfirmAssistantActionCommand(1, "user-1"), CancellationToken.None);

        Assert.Equal(ConfirmAssistantActionOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_DifferentUserThanRequested_ReturnsForbidden()
    {
        _pendingActionRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(NewPendingAction());

        var result = await CreateHandler().Handle(new ConfirmAssistantActionCommand(1, "someone-else"), CancellationToken.None);

        Assert.Equal(ConfirmAssistantActionOutcome.Forbidden, result.Outcome);
        _executor.Verify(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmed_ReturnsSameSuccessWithoutReExecuting()
    {
        _pendingActionRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewPendingAction(confirmedAt: DateTimeOffset.UtcNow.AddMinutes(-1)));

        var result = await CreateHandler().Handle(new ConfirmAssistantActionCommand(1, "user-1"), CancellationToken.None);

        Assert.Equal(ConfirmAssistantActionOutcome.AlreadyConfirmed, result.Outcome);
        _executor.Verify(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Expired_ReturnsExpired()
    {
        _pendingActionRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewPendingAction(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1)));

        var result = await CreateHandler().Handle(new ConfirmAssistantActionCommand(1, "user-1"), CancellationToken.None);

        Assert.Equal(ConfirmAssistantActionOutcome.Expired, result.Outcome);
        _executor.Verify(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Defense in depth: the flag is re-checked here even though a Propose* tool would already have
    // refused to create a proposal while disabled -- it could have been flipped off in between.
    [Fact]
    public async Task Handle_ActionsDisabled_ReturnsFeatureDisabled_EvenForAnOtherwiseValidPendingAction()
    {
        _pendingActionRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(NewPendingAction());

        var result = await CreateHandler(actionsEnabled: false).Handle(new ConfirmAssistantActionCommand(1, "user-1"), CancellationToken.None);

        Assert.Equal(ConfirmAssistantActionOutcome.FeatureDisabled, result.Outcome);
        _executor.Verify(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HappyPath_ExecutesOnce_MarksConfirmed_WritesAuditLog()
    {
        var pendingAction = NewPendingAction();
        _pendingActionRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pendingAction);
        _executor.Setup(e => e.ActionType).Returns(AssistantActionType.SetPrice);
        _executor
            .Setup(e => e.ExecuteAsync(pendingAction.ParametersJson, "user-1", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PendingActionExecutionResult(true, "Цена обновлена."));

        var result = await CreateHandler().Handle(new ConfirmAssistantActionCommand(1, "user-1"), CancellationToken.None);

        Assert.Equal(ConfirmAssistantActionOutcome.Confirmed, result.Outcome);
        Assert.NotNull(pendingAction.ConfirmedAt);
        _executor.Verify(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a => a.PerformedByUserId == "user-1" && a.Action.Contains("SetPrice"))), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExecutorReportsFailure_ReturnsExecutionFailed_DoesNotMarkConfirmed()
    {
        var pendingAction = NewPendingAction();
        _pendingActionRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pendingAction);
        _executor.Setup(e => e.ActionType).Returns(AssistantActionType.SetPrice);
        _executor
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PendingActionExecutionResult(false, "Товар не найден."));

        var result = await CreateHandler().Handle(new ConfirmAssistantActionCommand(1, "user-1"), CancellationToken.None);

        Assert.Equal(ConfirmAssistantActionOutcome.ExecutionFailed, result.Outcome);
        Assert.Null(pendingAction.ConfirmedAt);
        _auditLogRepository.Verify(r => r.Add(It.IsAny<AuditLog>()), Times.Never);
    }
}
