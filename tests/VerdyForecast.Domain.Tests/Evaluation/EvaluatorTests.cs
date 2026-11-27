using Microsoft.Extensions.Time.Testing;
using VerdyForecast.Domain.Evaluation;
using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Predictions;
using VerdyForecast.Domain.Teams;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Evaluation;

public class EvaluatorTests
{
    private const double Tolerance = 1e-6;
    private const int MatchIdValue = 10;
    private static readonly TeamId _verdy = new(1);
    private static readonly TeamId _opponent = new(2);
    private static readonly TeamId _otherTeam = new(3);

    // ---- 自分の予想 ----

    [Fact]
    public void EvaluateMyPrediction_WinWithoutPercentages_UsesRuleV1Probabilities()
    {
        var match = VerdyHomeMatch();
        var prediction = Predict(match, TeamOutcome.Win, Make.TeamScoreOf(2, 1));
        match.RecordResult(Make.ScoreOf(2, 1)).ShouldSucceed();

        var evaluation = Evaluator.EvaluateMyPrediction(prediction, match, _verdy).ShouldSucceed();

        // 60 / 25 / 15 で、結果が勝ち: RPS = ½ ×｛(0.6 − 1)² + (0.85 − 1)²｝ = 0.09125
        evaluation.Outcome.Rps.ShouldBe(0.09125, Tolerance);
        evaluation.Outcome.Brier.ShouldBe(0.245, Tolerance);
        evaluation.Outcome.LogLoss.ShouldBe(-Math.Log(0.6), Tolerance);
        evaluation.Outcome.Hit.ShouldBeTrue();
        evaluation.Score.ShouldBe(new PredictedScoreResult(ExactScore: true, GoalDifference: true));
    }

    [Fact]
    public void EvaluateMyPrediction_VerdyAway_UsesVerdyPerspective()
    {
        var match = Make.ScheduledMatch(_opponent, _verdy).WithId(MatchIdValue);
        var prediction = Predict(match, TeamOutcome.Win, Make.TeamScoreOf(2, 1));
        // ホームから見ると 1-2。ヴェルディから見ると 2-1 の勝ち
        match.RecordResult(Make.ScoreOf(1, 2)).ShouldSucceed();

        var evaluation = Evaluator.EvaluateMyPrediction(prediction, match, _verdy).ShouldSucceed();

        evaluation.Outcome.Rps.ShouldBe(0.09125, Tolerance);
        evaluation.Outcome.Hit.ShouldBeTrue();
        evaluation.Score.ShouldBe(new PredictedScoreResult(ExactScore: true, GoalDifference: true));
    }

    [Fact]
    public void EvaluateMyPrediction_TiedPercentages_HitUsesChoice()
    {
        var match = VerdyHomeMatch();
        var prediction = Predict(match, TeamOutcome.Win, Make.TeamScoreOf(1, 0), Make.PercentagesOf(40, 40, 20));
        match.RecordResult(Make.ScoreOf(1, 1)).ShouldSucceed();

        var evaluation = Evaluator.EvaluateMyPrediction(prediction, match, _verdy).ShouldSucceed();

        // 引き分けも 40% で並んでいるが、3択では勝ちを選んだので的中ではない
        evaluation.Outcome.Hit.ShouldBeFalse();
        evaluation.Outcome.Rps.ShouldBe(0.1, Tolerance);
        evaluation.Score.ShouldBe(new PredictedScoreResult(ExactScore: false, GoalDifference: false));
    }

    [Fact]
    public void EvaluateMyPrediction_SameGoalDifference_IsNotExactButGoalDifference()
    {
        var match = VerdyHomeMatch();
        var prediction = Predict(match, TeamOutcome.Win, Make.TeamScoreOf(2, 1));
        match.RecordResult(Make.ScoreOf(3, 2)).ShouldSucceed();

        Evaluator.EvaluateMyPrediction(prediction, match, _verdy).ShouldSucceed()
            .Score.ShouldBe(new PredictedScoreResult(ExactScore: false, GoalDifference: true));
    }

    [Fact]
    public void EvaluateMyPrediction_MatchNotFinished_Fails()
    {
        var match = VerdyHomeMatch();
        var prediction = Predict(match, TeamOutcome.Win, Make.TeamScoreOf(2, 1));

        Evaluator.EvaluateMyPrediction(prediction, match, _verdy).ShouldFailWith(EvaluationErrors.MatchNotFinished);
    }

    [Fact]
    public void EvaluateMyPrediction_TeamNotInMatch_Fails()
    {
        var match = VerdyHomeMatch();
        var prediction = Predict(match, TeamOutcome.Win, Make.TeamScoreOf(2, 1));
        match.RecordResult(Make.ScoreOf(2, 1)).ShouldSucceed();

        Evaluator.EvaluateMyPrediction(prediction, match, _otherTeam).ShouldFailWith(EvaluationErrors.NotVerdyMatch);
    }

    [Fact]
    public void EvaluateMyPrediction_DifferentMatch_Throws()
    {
        var match = VerdyHomeMatch();
        var prediction = Predict(match, TeamOutcome.Win, Make.TeamScoreOf(2, 1));
        var otherMatch = Make.ScheduledMatch(_verdy, _opponent).WithId(MatchIdValue + 1);
        otherMatch.RecordResult(Make.ScoreOf(2, 1)).ShouldSucceed();

        Should.Throw<ArgumentException>(() => Evaluator.EvaluateMyPrediction(prediction, otherMatch, _verdy));
    }

    // ---- モデルとベースライン ----

    [Fact]
    public void EvaluateModel_UsesHomePerspectiveAndMostLikelyScore()
    {
        var match = VerdyHomeMatch();
        match.RecordResult(Make.ScoreOf(1, 1)).ShouldSucceed();

        var evaluation = Evaluator.EvaluateModel(Probabilities(0.5, 0.3, 0.2), Make.ScoreOf(1, 0), match).ShouldSucceed();

        evaluation.Outcome.Rps.ShouldBe(0.145, Tolerance);
        evaluation.Outcome.Brier.ShouldBe(0.78, Tolerance);
        evaluation.Outcome.Hit.ShouldBeFalse();
        evaluation.Score.ShouldBe(new PredictedScoreResult(ExactScore: false, GoalDifference: false));
    }

    [Fact]
    public void EvaluateModel_MatchNotFinished_Fails() =>
        Evaluator.EvaluateModel(Probabilities(0.5, 0.3, 0.2), Make.ScoreOf(1, 0), VerdyHomeMatch())
            .ShouldFailWith(EvaluationErrors.MatchNotFinished);

    [Fact]
    public void EvaluateBaseline_HasNoScoreResult()
    {
        var match = VerdyHomeMatch();
        match.RecordResult(Make.ScoreOf(2, 0)).ShouldSucceed();

        var evaluation = Evaluator.EvaluateBaseline(Baselines.Uniform, match).ShouldSucceed();

        evaluation.Outcome.Rps.ShouldBe(5.0 / 18, Tolerance);
        evaluation.Score.ShouldBeNull();
    }

    [Fact]
    public void EvaluateBaseline_MatchNotFinished_Fails() =>
        Evaluator.EvaluateBaseline(Baselines.Uniform, VerdyHomeMatch()).ShouldFailWith(EvaluationErrors.MatchNotFinished);

    // ---- テスト用の補助 ----

    private static Match VerdyHomeMatch() => Make.ScheduledMatch(_verdy, _opponent).WithId(MatchIdValue);

    // キックオフの1時間前に予想を入れる
    private static MyPrediction Predict(Match match, TeamOutcome choice, TeamScore score, OutcomePercentages? percentages = null) =>
        MyPrediction.Create(match, _verdy, choice, score, percentages, new FakeTimeProvider(match.Kickoff.Utc.AddHours(-1))).ShouldSucceed();

    private static MatchOutcomeProbabilities Probabilities(double homeWin, double draw, double awayWin) =>
        MatchOutcomeProbabilities.Create(homeWin, draw, awayWin).ShouldSucceed();
}
