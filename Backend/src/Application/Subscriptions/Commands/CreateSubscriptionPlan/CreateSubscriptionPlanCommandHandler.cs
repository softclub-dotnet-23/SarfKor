using System.Text.Json;
using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Subscriptions;
using Domain.ValueObjects;

namespace Application.Subscriptions.Commands.CreateSubscriptionPlan;

public sealed class CreateSubscriptionPlanCommandHandler(
    ISubscriptionPlanRepository subscriptionPlanRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateSubscriptionPlanCommand, CreateSubscriptionPlanResult>
{
    public async Task<CreateSubscriptionPlanResult> Handle(CreateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        if (await subscriptionPlanRepository.GetByCodeAsync(command.Code, cancellationToken) is not null)
            return new CreateSubscriptionPlanResult(CreateSubscriptionPlanOutcome.CodeAlreadyExists, null);

        var plan = new SubscriptionPlan
        {
            Name = command.Name,
            Code = command.Code,
            MonthlyPrice = new Money(command.MonthlyPriceAmount, command.MonthlyPriceCurrency),
            MaxStores = command.MaxStores,
            MaxEmployees = command.MaxEmployees,
            FeaturesJson = command.Features is { Count: > 0 } ? JsonSerializer.Serialize(command.Features) : null,
            IsActive = true
        };

        subscriptionPlanRepository.Add(plan);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "SubscriptionPlan.Created",
            EntityType = nameof(SubscriptionPlan),
            EntityId = plan.Id,
            Details = $"{plan.Name} ({plan.Code}), {command.MonthlyPriceAmount} {command.MonthlyPriceCurrency}/mo",
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateSubscriptionPlanResult(CreateSubscriptionPlanOutcome.Created, plan.Id);
    }
}
