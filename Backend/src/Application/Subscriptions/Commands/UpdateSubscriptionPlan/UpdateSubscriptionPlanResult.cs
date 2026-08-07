namespace Application.Subscriptions.Commands.UpdateSubscriptionPlan;

public enum UpdateSubscriptionPlanOutcome
{
    Updated,
    NotFound
}

public sealed record UpdateSubscriptionPlanResult(UpdateSubscriptionPlanOutcome Outcome);
