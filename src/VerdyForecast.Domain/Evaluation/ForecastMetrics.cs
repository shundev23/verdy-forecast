using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Evaluation;

/// <summary>
/// 勝ち・分け・負けの確率の評価指標（評価手順書 9.1）。どれも小さいほど良い（的中は除く）。
/// RPS は結果の順番を逆にしても値が変わらず、Brier と対数損失は順番によらないので、
/// ホームから見た向きで計算しても、ヴェルディから見た向きで計算しても、同じ値になる。
/// </summary>
public sealed record ForecastScores(double Rps, double Brier, double LogLoss, bool Hit);

/// <summary>予想スコアの一致（評価手順書 9.1 の最後）。</summary>
public sealed record PredictedScoreResult(bool ExactScore, bool GoalDifference);

/// <summary>1試合の答え合わせの結果。ベースラインのように予想スコアがないときは、Score は null。</summary>
public sealed record PredictionEvaluation(ForecastScores Outcome, PredictedScoreResult? Score);

/// <summary>評価指標の計算。</summary>
public static class ForecastMetrics
{
    /// <summary>対数損失を計算するときの確率の下限（確率が 0 でも無限大にならないように）。</summary>
    public const double MinProbabilityForLogLoss = 1e-15;

    /// <summary>ホームから見た確率と結果で計算する。的中は、確率がいちばん高い結果（同じ確率で並んだら、そのどれか）が起きたとき。</summary>
    public static ForecastScores ForMatch(MatchOutcomeProbabilities probabilities, MatchOutcome actual)
    {
        ArgumentNullException.ThrowIfNull(probabilities);
        var actualIndex = actual switch
        {
            MatchOutcome.HomeWin => 0,
            MatchOutcome.Draw => 1,
            MatchOutcome.AwayWin => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(actual), actual, "定義されていない結果です。"),
        };
        return Calculate(probabilities.HomeWin, probabilities.Draw, probabilities.AwayWin, actualIndex);
    }

    /// <summary>あるチームから見た確率と結果で計算する。的中の決め方は <see cref="ForMatch"/> と同じ。</summary>
    public static ForecastScores ForTeam(TeamOutcomeProbabilities probabilities, TeamOutcome actual)
    {
        ArgumentNullException.ThrowIfNull(probabilities);
        var actualIndex = actual switch
        {
            TeamOutcome.Win => 0,
            TeamOutcome.Draw => 1,
            TeamOutcome.Loss => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(actual), actual, "定義されていない結果です。"),
        };
        return Calculate(probabilities.Win, probabilities.Draw, probabilities.Loss, actualIndex);
    }

    /// <summary>ホームから見たスコアどうしを比べる。</summary>
    public static PredictedScoreResult CompareScores(Score predicted, Score actual)
    {
        ArgumentNullException.ThrowIfNull(predicted);
        ArgumentNullException.ThrowIfNull(actual);
        return new PredictedScoreResult(
            ExactScore: predicted == actual,
            GoalDifference: predicted.Home - predicted.Away == actual.Home - actual.Away);
    }

    /// <summary>あるチームから見たスコアどうしを比べる。</summary>
    public static PredictedScoreResult CompareScores(TeamScore predicted, TeamScore actual)
    {
        ArgumentNullException.ThrowIfNull(predicted);
        ArgumentNullException.ThrowIfNull(actual);
        return new PredictedScoreResult(
            ExactScore: predicted == actual,
            GoalDifference: predicted.GoalsFor - predicted.GoalsAgainst == actual.GoalsFor - actual.GoalsAgainst);
    }

    // p1〜p3 は順番のある3つの結果の確率、actualIndex は起きた結果の位置（0〜2）
    private static ForecastScores Calculate(double p1, double p2, double p3, int actualIndex)
    {
        double[] p = [p1, p2, p3];
        double[] o = [0, 0, 0];
        o[actualIndex] = 1;

        // RPS = ½ ×｛(p₁ − o₁)² + (p₁ + p₂ − o₁ − o₂)²｝
        var rps = 0.5 * (Square(p[0] - o[0]) + Square(p[0] + p[1] - o[0] - o[1]));
        var brier = Square(p[0] - o[0]) + Square(p[1] - o[1]) + Square(p[2] - o[2]);
        var logLoss = -Math.Log(Math.Max(p[actualIndex], MinProbabilityForLogLoss));
        var hit = p[actualIndex] >= p.Max();
        return new ForecastScores(rps, brier, logLoss, hit);
    }

    private static double Square(double value) => value * value;
}
