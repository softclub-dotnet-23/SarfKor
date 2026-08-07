using System.Text.Json;
using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Subscriptions;
using Domain.ValueObjects;

namespace Application.Subscriptions.Commands.UpdateSubscriptionPlan;

public sealed class UpdateSubscriptionPlanCommandHandler(
    ISubscriptionPlanRepository subscriptionPlanRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateSubscriptionPlanCommand, UpdateSubscriptionPlanResult>
{
    public async Task<UpdateSubscriptionPlanResult> Handle(UpdateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await subscriptionPlanRepository.GetByIdAsync(command.SubscriptionPlanId, cancellationToken);
        if (plan is null)
            return new UpdateSubscriptionPlanResult(UpdateSubscriptionPlanOutcome.NotFound);

        // JsonSerializer.Serialize, not hand-rolled string interpolation, for both snapshots — a
        // plan Name containing a quote would otherwise break the JSON, and interpolating a decimal
        // directly is culture-sensitive (CurrentCulture can format 50 as "50,00" on a comma-decimal
        // machine, which isn't valid JSON at all).
        var before = JsonSerializer.Serialize(new { name = plan.Name, monthlyPrice = plan.MonthlyPrice.Amount, isActive = plan.IsActive });

        plan.Name = command.Name;
        plan.MonthlyPrice = new Money(command.MonthlyPriceAmount, command.MonthlyPriceCurrency);
        plan.MaxStores = command.MaxStores;
        plan.MaxEmployees = command.MaxEmployees;
        plan.FeaturesJson = command.Features is { Count: > 0 } ? JsonSerializer.Serialize(command.Features) : null;
        // IsActive=false ("hidden") only stops new assignment (ApproveStoreCommandHandler filters to
        // active plans) — StoreSubscriptions already issued against this plan are untouched, per
        // ADMIN_PROMPT.md §2.1.
        plan.IsActive = command.IsActive;

        var after = JsonSerializer.Serialize(new { name = plan.Name, monthlyPrice = plan.MonthlyPrice.Amount, isActive = plan.IsActive });

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "SubscriptionPlan.Updated",
            EntityType = nameof(SubscriptionPlan),
            EntityId = plan.Id,
            IpAddress = command.PerformedByIpAddress,
            BeforeStateJson = before,
            AfterStateJson = after,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new UpdateSubscriptionPlanResult(UpdateSubscriptionPlanOutcome.Updated);
    }
}
