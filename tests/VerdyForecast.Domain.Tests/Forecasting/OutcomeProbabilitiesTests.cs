using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Forecasting;

public class OutcomeProbabilitiesTests
{
    [Theory]
    [InlineData(0.5, 0.3, 0.2)]
    [InlineData(1.0, 0.0, 0.0)]
    [InlineData(0.5, 0.3, 0.2000000001)]
    public void Create_ValidProbabilities_Succeeds(double homeWin, double draw, double awayWin)
    {
        var probabilities = MatchOutcomeProbabilities.Create(homeWin, draw, awayWin).ShouldSucceed();

        probabilities.HomeWin.ShouldBe(homeWin);
        probabilities.Draw.ShouldBe(draw);
        probabilities.AwayWin.ShouldBe(awayWin);
    }

    [Theory]
    [InlineData(0.5, 0.3, 0.3)]
    [InlineData(0.5, 0.3, 0.19999999)]
    [InlineData(-0.1, 0.6, 0.5)]
    [InlineData(1.1, -0.05, -0.05)]
    [InlineData(double.NaN, 0.5, 0.5)]
    public void Create_InvalidProbabilities_Fails(double homeWin, double draw, double awayWin) =>
        MatchOutcomeProbabilities.Create(homeWin, draw, awayWin).ShouldFailWith(ForecastErrors.InvalidProbabilities);

    [Fact]
    public void ForTeam_Home_KeepsOrder()
    {
        var forHome = MatchOutcomeProbabilities.Create(0.5, 0.3, 0.2).ShouldSucceed().ForTeam(Side.Home);

        forHome.Win.ShouldBe(0.5);
        forHome.Draw.ShouldBe(0.3);
        forHome.Loss.ShouldBe(0.2);
    }

    [Fact]
    public void ForTeam_Away_SwapsWinAndLoss()
    {
        var forAway = MatchOutcomeProbabilities.Create(0.5, 0.3, 0.2).ShouldSucceed().ForTeam(Side.Away);

        forAway.Win.ShouldBe(0.2);
        forAway.Draw.ShouldBe(0.3);
        forAway.Loss.ShouldBe(0.5);
    }

    [Fact]
    public void ForTeam_UndefinedSide_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => MatchOutcomeProbabilities.Create(0.5, 0.3, 0.2).ShouldSucceed().ForTeam((Side)99));

    [Fact]
    public void TeamOutcomeProbabilities_Create_ValidProbabilities_Succeeds()
    {
        var probabilities = TeamOutcomeProbabilities.Create(0.6, 0.25, 0.15).ShouldSucceed();

        probabilities.Win.ShouldBe(0.6);
        probabilities.Draw.ShouldBe(0.25);
        probabilities.Loss.ShouldBe(0.15);
    }

    [Fact]
    public void TeamOutcomeProbabilities_Create_SumNotOne_Fails() =>
        TeamOutcomeProbabilities.Create(0.6, 0.25, 0.25).ShouldFailWith(ForecastErrors.InvalidProbabilities);
}
