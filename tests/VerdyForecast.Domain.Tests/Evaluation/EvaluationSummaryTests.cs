using VerdyForecast.Domain.Evaluation;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Evaluation;

public class EvaluationSummaryTests
{
    private const double Tolerance = 1e-12;

    [Fact]
    public void From_TwoEvaluations_AveragesAndCounts()
    {
        PredictionEvaluation[] evaluations =
        [
            new(new ForecastScores(Rps: 0.1, Brier: 0.2, LogLoss: 0.3, Hit: true), new PredictedScoreResult(ExactScore: true, GoalDifference: true)),
            new(new ForecastScores(Rps: 0.3, Brier: 0.4, LogLoss: 0.5, Hit: false), new PredictedScoreResult(ExactScore: false, GoalDifference: true)),
        ];

        var summary = EvaluationSummary.From(evaluations).ShouldSucceed();

        summary.Count.ShouldBe(2);
        summary.MeanRps.ShouldBe(0.2, Tolerance);
        summary.MeanBrier.ShouldBe(0.3, Tolerance);
        summary.MeanLogLoss.ShouldBe(0.4, Tolerance);
        summary.Hits.ShouldBe(1);
        summary.HitRate.ShouldBe(0.5, Tolerance);
        summary.ScoredCount.ShouldBe(2);
        summary.ExactScores.ShouldBe(1);
        summary.GoalDifferences.ShouldBe(2);
    }

    [Fact]
    public void From_WithoutScoreResults_CountsOnlyScoredForScoreMatches()
    {
        PredictionEvaluation[] evaluations =
        [
            new(new ForecastScores(0.2, 0.3, 0.4, Hit: true), new PredictedScoreResult(ExactScore: true, GoalDifference: true)),
            new(new ForecastScores(0.2, 0.3, 0.4, Hit: true), Score: null),
            new(new ForecastScores(0.2, 0.3, 0.4, Hit: false), Score: null),
        ];

        var summary = EvaluationSummary.From(evaluations).ShouldSucceed();

        summary.Count.ShouldBe(3);
        summary.Hits.ShouldBe(2);
        summary.ScoredCount.ShouldBe(1);
        summary.ExactScores.ShouldBe(1);
        summary.GoalDifferences.ShouldBe(1);
    }

    [Fact]
    public void From_NoEvaluations_Fails() =>
        EvaluationSummary.From([]).ShouldFailWith(EvaluationErrors.NoEvaluations);
}
