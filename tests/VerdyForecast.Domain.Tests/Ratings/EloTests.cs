using VerdyForecast.Domain.Ratings;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Ratings;

// 期待値は電卓で手計算した値。例: 1 ÷ (1 + 10^(-50 ÷ 400)) ≒ 0.571463
public class EloTests
{
    private const double Tolerance = 1e-4;

    [Fact]
    public void ExpectedHomeScore_EqualRatingsWithoutHomeAdvantage_IsHalf() =>
        Elo.ExpectedHomeScore(1500, 1500, homeAdvantage: 0).ShouldBe(0.5, Tolerance);

    [Fact]
    public void ExpectedHomeScore_EqualRatingsWithHomeAdvantage50_FavorsHome() =>
        Elo.ExpectedHomeScore(1500, 1500, homeAdvantage: 50).ShouldBe(0.571463, Tolerance);

    [Fact]
    public void ExpectedHomeScore_FourHundredPointsStronger_IsTenToOne() =>
        Elo.ExpectedHomeScore(1900, 1500, homeAdvantage: 0).ShouldBe(10.0 / 11.0, Tolerance);

    [Fact]
    public void ExpectedHomeScore_SwappedTeamsWithoutHomeAdvantage_SumToOne() =>
        (Elo.ExpectedHomeScore(1620, 1480, 0) + Elo.ExpectedHomeScore(1480, 1620, 0)).ShouldBe(1.0, Tolerance);

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(1, 1.0)]
    [InlineData(-1, 1.0)]
    [InlineData(2, 1.5)]
    [InlineData(-2, 1.5)]
    [InlineData(3, 1.75)]
    [InlineData(4, 1.875)]
    [InlineData(5, 2.0)]
    public void GoalDifferenceMultiplier_FollowsD4(int goalDifference, double expected) =>
        Elo.GoalDifferenceMultiplier(goalDifference).ShouldBe(expected);

    [Fact]
    public void HomeRatingChange_EqualTeamsHomeWinsByOne_IsAboutPlus8_57() =>
        Elo.HomeRatingChange(1500, 1500, Make.ScoreOf(1, 0), EloSettings.Default).ShouldBe(8.5707, Tolerance);

    [Fact]
    public void HomeRatingChange_EqualTeamsDraw_HomeLosesALittle() =>
        Elo.HomeRatingChange(1500, 1500, Make.ScoreOf(1, 1), EloSettings.Default).ShouldBe(-1.4293, Tolerance);

    [Fact]
    public void HomeRatingChange_AwayWinsByTwo_UsesMultiplier() =>
        Elo.HomeRatingChange(1500, 1500, Make.ScoreOf(0, 2), EloSettings.Default).ShouldBe(-17.1439, Tolerance);

    [Fact]
    public void HomeRatingChange_UsesKAndHomeAdvantageFromSettings()
    {
        var settings = EloSettings.Create(k: 40, homeAdvantage: 0, carryOver: 1).ShouldSucceed();

        Elo.HomeRatingChange(1500, 1500, Make.ScoreOf(2, 1), settings).ShouldBe(20.0, Tolerance);
    }
}
