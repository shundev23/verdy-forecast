using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Forecasting;

// 期待値は手計算。例: λ = 1 と 1 なら P(0-0) = e^(−2) ≒ 0.135335、
// 引き分けの確率 = e^(−2) × Σ 1 ÷ (k!)² ≒ 0.135335 × 2.279585 ≒ 0.308508
public class ScoreDistributionTests
{
    [Fact]
    public void Probability_LambdaOneAndOne_ZeroZeroIsEToMinusTwo() =>
        Distribution(1.0, 1.0).Probability(0, 0).ShouldBe(Math.Exp(-2), 1e-6);

    [Fact]
    public void Probability_LambdaOnePointFiveAndOne_OneZeroMatchesPoisson() =>
        Distribution(1.5, 1.0).Probability(1, 0).ShouldBe(0.1231276, 1e-6);

    [Fact]
    public void All_ProbabilitiesSumToOne() =>
        Distribution(1.7, 0.9).All.Sum(s => s.Probability).ShouldBe(1.0, 1e-12);

    [Fact]
    public void All_Has121ScoresOrderedByHomeThenAway()
    {
        var all = Distribution(1.2, 1.2).All;

        all.Count.ShouldBe(121);
        all[0].Score.ShouldBe(Make.ScoreOf(0, 0));
        all[1].Score.ShouldBe(Make.ScoreOf(0, 1));
        all[11].Score.ShouldBe(Make.ScoreOf(1, 0));
        all[120].Score.ShouldBe(Make.ScoreOf(10, 10));
    }

    [Fact]
    public void Outcomes_LambdaOneAndOne_MatchHandCalculation()
    {
        var outcomes = Distribution(1.0, 1.0).Outcomes;

        outcomes.Draw.ShouldBe(0.308508, 1e-5);
        outcomes.HomeWin.ShouldBe(0.345746, 1e-5);
        outcomes.AwayWin.ShouldBe(0.345746, 1e-5);
    }

    [Fact]
    public void Outcomes_AreSumsOfScoreProbabilities()
    {
        var distribution = Distribution(1.6, 1.1);
        double homeWin = 0, draw = 0, awayWin = 0;
        foreach (var cell in distribution.All)
        {
            if (cell.Score.Home > cell.Score.Away)
            {
                homeWin += cell.Probability;
            }
            else if (cell.Score.Home == cell.Score.Away)
            {
                draw += cell.Probability;
            }
            else
            {
                awayWin += cell.Probability;
            }
        }

        distribution.Outcomes.HomeWin.ShouldBe(homeWin, 1e-12);
        distribution.Outcomes.Draw.ShouldBe(draw, 1e-12);
        distribution.Outcomes.AwayWin.ShouldBe(awayWin, 1e-12);
        (distribution.Outcomes.HomeWin + distribution.Outcomes.Draw + distribution.Outcomes.AwayWin).ShouldBe(1.0, 1e-12);
    }

    [Fact]
    public void Outcomes_SameLambdas_HomeWinEqualsAwayWin()
    {
        var outcomes = Distribution(1.3, 1.3).Outcomes;

        outcomes.HomeWin.ShouldBe(outcomes.AwayWin, 1e-12);
    }

    [Fact]
    public void Outcomes_HigherHomeLambda_HomeWinIsMoreLikely()
    {
        var outcomes = Distribution(2.0, 0.8).Outcomes;

        outcomes.HomeWin.ShouldBeGreaterThan(outcomes.AwayWin);
    }

    [Fact]
    public void MostLikely_TiedProbabilities_OrderedByHomeThenAway()
    {
        // λ = 1 と 1 では、0-0、0-1、1-0、1-1 がすべて e^(−2) で同じ確率
        var top = Distribution(1.0, 1.0).MostLikely(4);

        top.Select(s => s.Score).ShouldBe([Make.ScoreOf(0, 0), Make.ScoreOf(0, 1), Make.ScoreOf(1, 0), Make.ScoreOf(1, 1)]);
    }

    [Fact]
    public void MostLikely_ReturnsHighestFirst()
    {
        var top = Distribution(2.2, 0.6).MostLikely(3);

        top.Count.ShouldBe(3);
        top[0].Probability.ShouldBeGreaterThanOrEqualTo(top[1].Probability);
        top[1].Probability.ShouldBeGreaterThanOrEqualTo(top[2].Probability);
        top[0].Probability.ShouldBe(Distribution(2.2, 0.6).All.Max(s => s.Probability));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(122)]
    public void MostLikely_CountOutOfRange_Throws(int count) =>
        Should.Throw<ArgumentOutOfRangeException>(() => Distribution(1.0, 1.0).MostLikely(count));

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(11, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 11)]
    public void Probability_GoalsOutOfRange_Throws(int homeGoals, int awayGoals) =>
        Should.Throw<ArgumentOutOfRangeException>(() => Distribution(1.0, 1.0).Probability(homeGoals, awayGoals));

    private static ScoreDistribution Distribution(double home, double away) =>
        ScoreDistribution.FromExpectedGoals(ExpectedGoals.Create(home, away).ShouldSucceed());
}
