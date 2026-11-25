using VerdyForecast.Domain.Evaluation;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Evaluation;

public class BaselinesTests
{
    [Fact]
    public void Uniform_IsOneThirdEach()
    {
        Baselines.Uniform.HomeWin.ShouldBe(1.0 / 3, 1e-12);
        Baselines.Uniform.Draw.ShouldBe(1.0 / 3, 1e-12);
        Baselines.Uniform.AwayWin.ShouldBe(1.0 / 3, 1e-12);
    }

    [Fact]
    public void LeagueFrequency_PastResults_ReturnsShares()
    {
        var frequency = Baselines.LeagueFrequency([MatchOutcome.HomeWin, MatchOutcome.HomeWin, MatchOutcome.Draw, MatchOutcome.AwayWin]).ShouldSucceed();

        frequency.HomeWin.ShouldBe(0.5, 1e-12);
        frequency.Draw.ShouldBe(0.25, 1e-12);
        frequency.AwayWin.ShouldBe(0.25, 1e-12);
    }

    [Fact]
    public void LeagueFrequency_OneResult_IsCertain()
    {
        var frequency = Baselines.LeagueFrequency([MatchOutcome.Draw]).ShouldSucceed();

        frequency.HomeWin.ShouldBe(0);
        frequency.Draw.ShouldBe(1);
        frequency.AwayWin.ShouldBe(0);
    }

    [Fact]
    public void LeagueFrequency_NoResults_Fails() =>
        Baselines.LeagueFrequency([]).ShouldFailWith(EvaluationErrors.NoPastResults);

    [Fact]
    public void LeagueFrequency_UndefinedResult_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Baselines.LeagueFrequency([MatchOutcome.Draw, (MatchOutcome)99]));
}
