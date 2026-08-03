using Application.Abstractions;
using Application.Assistant.Abstractions;
using Application.Common;
using Domain.Auditing;
using Microsoft.Extensions.Options;

namespace Application.Assistant.Commands.ConfirmAssistantAction;

/// <summary>
/// The only place a Mode C proposal actually becomes a real mutation -- always a separate,
/// explicitly-confirmed request from whatever chat turn created the proposal (never triggered by
/// the chat loop itself). See AssistantOptions.ActionsEnabled for why the flag is re-checked here,
/// not just when AssistantToolRegistry decided whether to offer the Propose* tool in the first place.
/// </summary>
public sealed class ConfirmAssistantActionCommandHandler(
    IPendingAssistantActionRepository pendingActionRepository,
    IEnumerable<IPendingActionExecutor> executors,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    IOptions<AssistantOptions> options) : ICommandHandler<ConfirmAssistantActionCommand, ConfirmAssistantActionResult>
{
    public async Task<ConfirmAssistantActionResult> Handle(ConfirmAssistantActionCommand command, CancellationToken cancellationToken)
    {
        var pendingAction = await pendingActionRepository.GetByIdAsync(command.PendingActionId, cancellationToken);
        if (pendingAction is null)
            return new ConfirmAssistantActionResult(ConfirmAssistantActionOutcome.NotFound, null);

        // Only whoever the assistant proposed this to can confirm it -- the underlying executor
        // (e.g. SubmitPriceUpdateCommandHandler) re-checks store ownership/employment independently
        // on top of this, so this isn't the only authorization layer, just the first one.
        if (pendingAction.RequestedByUserId != command.UserId)
            return new ConfirmAssistantActionResult(ConfirmAssistantActionOutcome.Forbidden, null);

        if (pendingAction.ConfirmedAt is not null)
            return new ConfirmAssistantActionResult(ConfirmAssistantActionOutcome.AlreadyConfirmed, pendingAction.Summary);

        if (DateTimeOffset.UtcNow > pendingAction.ExpiresAt)
            return new ConfirmAssistantActionResult(ConfirmAssistantActionOutcome.Expired, null);

        if (!options.Value.ActionsEnabled)
            return new ConfirmAssistantActionResult(ConfirmAssistantActionOutcome.FeatureDisabled, null);

        var executor = executors.FirstOrDefault(e => e.ActionType == pendingAction.ActionType);
        if (executor is null)
            return new ConfirmAssistantActionResult(ConfirmAssistantActionOutcome.ExecutionFailed, null);

        var executionResult = await executor.ExecuteAsync(pendingAction.ParametersJson, pendingAction.RequestedByUserId, pendingAction.StoreId, cancellationToken);
        if (!executionResult.Success)
            return new ConfirmAssistantActionResult(ConfirmAssistantActionOutcome.ExecutionFailed, executionResult.Summary);

        pendingAction.ConfirmedAt = DateTimeOffset.UtcNow;
        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.UserId,
            Action = $"Assistant.{pendingAction.ActionType}.Confirmed",
            EntityType = nameof(Domain.Assistant.PendingAssistantAction),
            EntityId = pendingAction.Id,
            Details = pendingAction.ParametersJson,
            OccurredAt = DateTimeOffset.UtcNow,
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConfirmAssistantActionResult(ConfirmAssistantActionOutcome.Confirmed, executionResult.Summary);
    }
}
