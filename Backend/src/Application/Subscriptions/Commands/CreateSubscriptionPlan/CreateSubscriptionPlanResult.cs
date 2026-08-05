namespace Application.Subscriptions.Commands.CreateSubscriptionPlan;

public enum CreateSubscriptionPlanOutcome
{
    Created,
    CodeAlreadyExists
}

public sealed record CreateSubscriptionPlanResult(CreateSubscriptionPlanOutcome Outcome, int? SubscriptionPlanId);
