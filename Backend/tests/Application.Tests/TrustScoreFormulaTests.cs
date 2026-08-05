using Application.Reputation;

namespace Application.Tests;

public class TrustScoreFormulaTests
{
    // Nightly mean-reversion toward DefaultScore (50), not toward zero — ADMIN_PROMPT.md §2.4:
    // "давние события весят меньше свежих" without replaying history.
    [Theory]
    [InlineData(80, true)] // above default -> decays down toward 50
    [InlineData(20, false)] // below default -> decays up toward 50
    public void ApplyDailyDecay_MovesScoreTowardDefault(double score, bool shouldDecrease)
    {
        var decayed = TrustScoreFormula.ApplyDailyDecay(score);

        if (shouldDecrease)
            Assert.True(decayed < score);
        else
            Assert.True(decayed > score);
    }

    [Fact]
    public void ApplyDailyDecay_AtDefaultScore_StaysAtDefault()
    {
        Assert.Equal(TrustScoreFormula.DefaultScore, TrustScoreFormula.ApplyDailyDecay(TrustScoreFormula.DefaultScore));
    }

    [Fact]
    public void ApplyDailyDecay_RepeatedApplication_ConvergesTowardDefaultWithoutOvershooting()
    {
        var score = 100.0;
        for (var day = 0; day < 5000; day++)
            score = TrustScoreFormula.ApplyDailyDecay(score);

        Assert.True(Math.Abs(score - TrustScoreFormula.DefaultScore) < 0.01);
    }

    [Fact]
    public void PricesCorroborate_BothZero_ReturnsTrue()
    {
        Assert.True(TrustScoreFormula.PricesCorroborate(0, 0));
    }

    [Fact]
    public void PricesCorroborate_WithinTolerance_ReturnsTrue()
    {
        // 100 vs 104 is within the 5% tolerance of the larger value.
        Assert.True(TrustScoreFormula.PricesCorroborate(100m, 104m));
    }

    [Fact]
    public void PricesCorroborate_OutsideTolerance_ReturnsFalse()
    {
        // 100 vs 120 is a 20% gap, well past the 5% tolerance.
        Assert.False(TrustScoreFormula.PricesCorroborate(100m, 120m));
    }

    // Asymmetric penalty: a refuted price must cost strictly more than a confirmed price earns —
    // the whole point of discouraging bad-faith submissions rather than just rewarding good ones.
    [Fact]
    public void RefutedDelta_IsLargerInMagnitudeThanConfirmedDelta()
    {
        Assert.True(Math.Abs(TrustScoreFormula.PriceRefutedDelta) > Math.Abs(TrustScoreFormula.PriceConfirmedDelta));
    }
}
