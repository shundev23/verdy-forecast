using VerdyForecast.Domain.Evaluation;
using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Evaluation;

public class ForecastMetricsTests
{
    private const double Tolerance = 1e-6;

    [Fact]
    public void ForTeam_EvaluationProcedureExample_MatchesDocumentedValues()
    {
        // 評価手順書 9.1 の計算例: 「勝ち50%・分け30%・負け20%」で、結果が引き分け
        var scores = ForecastMetrics.ForTeam(Team(0.5, 0.3, 0.2), TeamOutcome.Draw);

        scores.Rps.ShouldBe(0.145, Tolerance);
        scores.Brier.ShouldBe(0.78, Tolerance);
        scores.LogLoss.ShouldBe(-Math.Log(0.3), Tolerance);
        scores.LogLoss.ShouldBe(1.204, 0.001);
        scores.Hit.ShouldBeFalse();
    }

    [Fact]
    public void ForMatch_EvaluationProcedureExample_MatchesDocumentedValues()
    {
        var scores = ForecastMetrics.ForMatch(Match(0.5, 0.3, 0.2), MatchOutcome.Draw);

        scores.Rps.ShouldBe(0.145, Tolerance);
        scores.Brier.ShouldBe(0.78, Tolerance);
        scores.LogLoss.ShouldBe(-Math.Log(0.3), Tolerance);
        scores.Hit.ShouldBeFalse();
    }

    [Fact]
    public void ForTeam_CertainAndCorrect_IsPerfect()
    {
        var scores = ForecastMetrics.ForTeam(Team(1, 0, 0), TeamOutcome.Win);

        scores.Rps.ShouldBe(0, Tolerance);
        scores.Brier.ShouldBe(0, Tolerance);
        scores.LogLoss.ShouldBe(0, Tolerance);
        scores.Hit.ShouldBeTrue();
    }

    [Fact]
    public void ForTeam_CertainButOppositeResult_IsWorst()
    {
        var scores = ForecastMetrics.ForTeam(Team(1, 0, 0), TeamOutcome.Loss);

        scores.Rps.ShouldBe(1, Tolerance);
        scores.Brier.ShouldBe(2, Tolerance);
        // 確率 0 は下限 1e-15 で計算するので、無限大にならない
        scores.LogLoss.ShouldBe(-Math.Log(ForecastMetrics.MinProbabilityForLogLoss), Tolerance);
        scores.Hit.ShouldBeFalse();
    }

    [Fact]
    public void ForMatch_Uniform_MatchesHandCalculation()
    {
        var scores = ForecastMetrics.ForMatch(Baselines.Uniform, MatchOutcome.HomeWin);

        scores.Rps.ShouldBe(5.0 / 18, Tolerance);
        scores.Brier.ShouldBe(2.0 / 3, Tolerance);
        scores.LogLoss.ShouldBe(Math.Log(3), Tolerance);
        scores.Hit.ShouldBeTrue();
    }

    [Theory]
    [InlineData(MatchOutcome.HomeWin, Side.Home)]
    [InlineData(MatchOutcome.HomeWin, Side.Away)]
    [InlineData(MatchOutcome.Draw, Side.Home)]
    [InlineData(MatchOutcome.Draw, Side.Away)]
    [InlineData(MatchOutcome.AwayWin, Side.Home)]
    [InlineData(MatchOutcome.AwayWin, Side.Away)]
    public void ForTeam_SameMatchFromEitherPerspective_GivesSameScores(MatchOutcome actual, Side side)
    {
        var probabilities = Match(0.45, 0.30, 0.25);

        var fromHome = ForecastMetrics.ForMatch(probabilities, actual);
        var fromTeam = ForecastMetrics.ForTeam(probabilities.ForTeam(side), actual.ToTeamOutcome(side));

        fromTeam.Rps.ShouldBe(fromHome.Rps, 1e-12);
        fromTeam.Brier.ShouldBe(fromHome.Brier, 1e-12);
        fromTeam.LogLoss.ShouldBe(fromHome.LogLoss, 1e-12);
        fromTeam.Hit.ShouldBe(fromHome.Hit);
    }

    [Theory]
    [InlineData(TeamOutcome.Win, true)]
    [InlineData(TeamOutcome.Draw, true)]
    [InlineData(TeamOutcome.Loss, false)]
    public void ForTeam_TiedHighest_EitherTiedResultIsHit(TeamOutcome actual, bool expected) =>
        ForecastMetrics.ForTeam(Team(0.4, 0.4, 0.2), actual).Hit.ShouldBe(expected);

    [Fact]
    public void ForMatch_UndefinedOutcome_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => ForecastMetrics.ForMatch(Match(0.5, 0.3, 0.2), (MatchOutcome)99));

    [Fact]
    public void ForTeam_UndefinedOutcome_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => ForecastMetrics.ForTeam(Team(0.5, 0.3, 0.2), (TeamOutcome)99));

    [Theory]
    [InlineData(2, 1, 2, 1, true, true)]
    [InlineData(2, 1, 3, 2, false, true)]
    [InlineData(2, 1, 1, 1, false, false)]
    [InlineData(0, 0, 1, 1, false, true)]
    [InlineData(1, 2, 2, 1, false, false)]
    public void CompareScores_Score_ChecksExactAndGoalDifference(int predictedHome, int predictedAway, int actualHome, int actualAway, bool exact, bool goalDifference) =>
        ForecastMetrics.CompareScores(Make.ScoreOf(predictedHome, predictedAway), Make.ScoreOf(actualHome, actualAway))
            .ShouldBe(new PredictedScoreResult(exact, goalDifference));

    [Theory]
    [InlineData(2, 1, 2, 1, true, true)]
    [InlineData(2, 1, 3, 2, false, true)]
    [InlineData(1, 0, 1, 1, false, false)]
    public void CompareScores_TeamScore_ChecksExactAndGoalDifference(int predictedFor, int predictedAgainst, int actualFor, int actualAgainst, bool exact, bool goalDifference) =>
        ForecastMetrics.CompareScores(Make.TeamScoreOf(predictedFor, predictedAgainst), Make.TeamScoreOf(actualFor, actualAgainst))
            .ShouldBe(new PredictedScoreResult(exact, goalDifference));

    private static TeamOutcomeProbabilities Team(double win, double draw, double loss) =>
        TeamOutcomeProbabilities.Create(win, draw, loss).ShouldSucceed();

    private static MatchOutcomeProbabilities Match(double homeWin, double draw, double awayWin) =>
        MatchOutcomeProbabilities.Create(homeWin, draw, awayWin).ShouldSucceed();
}
