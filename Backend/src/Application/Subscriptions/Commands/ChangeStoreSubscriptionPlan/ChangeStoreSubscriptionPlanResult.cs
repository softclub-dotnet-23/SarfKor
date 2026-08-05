namespace Application.Subscriptions.Commands.ChangeStoreSubscriptionPlan;

public enum ChangeStoreSubscriptionPlanOutcome
{
    Changed,
    SubscriptionNotFound,
    PlanNotFound
}

public sealed record ChangeStoreSubscriptionPlanResult(ChangeStoreSubscriptionPlanOutcome Outcome);
