using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Evaluation;

/// <summary>
/// 複数の試合の答え合わせのまとめ（シーズンごとや全体の平均。評価手順書 9.3、9.6、画面 S4）。
/// </summary>
public sealed record EvaluationSummary
{
    private EvaluationSummary(
        int count,
        double meanRps,
        double meanBrier,
        double meanLogLoss,
        int hits,
        int scoredCount,
        int exactScores,
        int goalDifferences)
    {
        Count = count;
        MeanRps = meanRps;
        MeanBrier = meanBrier;
        MeanLogLoss = meanLogLoss;
        Hits = hits;
        ScoredCount = scoredCount;
        ExactScores = exactScores;
        GoalDifferences = goalDifferences;
    }

    /// <summary>試合の数。</summary>
    public int Count { get; }

    public double MeanRps { get; }

    public double MeanBrier { get; }

    public double MeanLogLoss { get; }

    /// <summary>的中した試合の数。</summary>
    public int Hits { get; }

    /// <summary>的中率（的中した試合の数 ÷ 試合の数）。</summary>
    public double HitRate => (double)Hits / Count;

    /// <summary>予想スコアがあった試合の数（ベースラインには予想スコアがない）。</summary>
    public int ScoredCount { get; }

    /// <summary>スコアが完全に一致した試合の数。</summary>
    public int ExactScores { get; }

    /// <summary>得失点差が一致した試合の数。</summary>
    public int GoalDifferences { get; }

    public static Result<EvaluationSummary> From(IReadOnlyCollection<PredictionEvaluation> evaluations)
    {
        ArgumentNullException.ThrowIfNull(evaluations);
        if (evaluations.Count == 0)
        {
            return Result.Failure<EvaluationSummary>(EvaluationErrors.NoEvaluations);
        }

        var scored = evaluations.Where(e => e.Score is not null).Select(e => e.Score!).ToList();
        return Result.Success(new EvaluationSummary(
            evaluations.Count,
            evaluations.Average(e => e.Outcome.Rps),
            evaluations.Average(e => e.Outcome.Brier),
            evaluations.Average(e => e.Outcome.LogLoss),
            evaluations.Count(e => e.Outcome.Hit),
            scored.Count,
            scored.Count(s => s.ExactScore),
            scored.Count(s => s.GoalDifference)));
    }
}
