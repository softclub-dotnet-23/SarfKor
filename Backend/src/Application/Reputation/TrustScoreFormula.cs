namespace Application.Reputation;

/// <summary>
/// The one place every ContributorTrustScore coefficient lives (ADMIN_PROMPT.md §2.4: "формулу
/// вынеси в одно место с явными коэффициентами-настройками"). Scale is centered on the existing
/// <c>DefaultTrustScore</c> a new contributor starts at (see SubmitPriceUpdateCommandHandler) —
/// 50 — not 0, so nothing about an already-neutral contributor's behavior changes just from this
/// mechanism existing.
/// </summary>
public static class TrustScoreFormula
{
    public const double DefaultScore = 50;

    /// <summary>A submission independently corroborated by a second, different-user submission.</summary>
    public const double PriceConfirmedDelta = +2;

    /// <summary>A submission an Admin/automatic signal later established was wrong. Asymmetric on
    /// purpose — a wrong price should cost more than a right one earns, the standard shape for
    /// discouraging bad-faith submissions rather than just rewarding good ones.</summary>
    public const double PriceRefutedDelta = -5;

    /// <summary>One Report landing against this user's contribution (product/price they submitted).</summary>
    public const double ReportAgainstAuthorDelta = -3;

    /// <summary>Below this, a new PriceEntry from this author starts unverified (hidden from public
    /// results) until a second independent submission corroborates it — see PriceEntry.IsVerified.</summary>
    public const double LowTrustThreshold = 30;

    /// <summary>Nightly mean-reversion toward DefaultScore, not toward zero — "давние события весят
    /// меньше свежих" without needing to replay/reweight historical events. A manual Admin
    /// adjustment decays along with everything else, which is intentional: an old manual correction
    /// fading over time (rather than remaining a permanent, non-reviewable override) matches the
    /// same "not silently overwritten by an automatic recalculation" rule as its opposite —
    /// automatic events don't erase manual ones either, both just decay at the same rate.</summary>
    public const double DailyDecayTowardDefault = 0.01;

    public static double ApplyDailyDecay(double score) => score + (DefaultScore - score) * DailyDecayTowardDefault;

    /// <summary>A newly submitted price is corroborated by a prior one when both concern the same
    /// product/store, come from two different users, and land within this tolerance of each other —
    /// the tolerance absorbs ordinary rounding/promo timing noise without accepting a wildly
    /// different number as confirmation.</summary>
    public const decimal CorroborationPriceTolerancePercent = 0.05m;

    public static bool PricesCorroborate(decimal a, decimal b)
    {
        if (a == 0 && b == 0) return true;
        var reference = Math.Max(Math.Abs(a), Math.Abs(b));
        return Math.Abs(a - b) / reference <= CorroborationPriceTolerancePercent;
    }
}
