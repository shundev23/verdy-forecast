using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Evaluation;

/// <summary>
/// 比べる相手（評価手順書 9.2）。モデルがこの2つに勝てないときは、画面のモデルの予想に「参考値」と書いて、モデルを見直す。
/// </summary>
public static class Baselines
{
    /// <summary>いつも勝ち・分け・負けを 1/3 ずつにする。</summary>
    public static MatchOutcomeProbabilities Uniform { get; } = MatchOutcomeProbabilities.FromValid(1.0 / 3, 1.0 / 3, 1.0 / 3);

    /// <summary>
    /// 過去の試合の、ホーム勝ち・引き分け・アウェイ勝ちの割合。
    /// 渡すのは、評価するシーズンより前の結果だけ（データリーケージの防止。選ぶのは呼ぶ側）。
    /// </summary>
    public static Result<MatchOutcomeProbabilities> LeagueFrequency(IReadOnlyCollection<MatchOutcome> pastResults)
    {
        ArgumentNullException.ThrowIfNull(pastResults);
        if (pastResults.Count == 0)
        {
            return Result.Failure<MatchOutcomeProbabilities>(EvaluationErrors.NoPastResults);
        }

        int homeWins = 0, draws = 0, awayWins = 0;
        foreach (var result in pastResults)
        {
            switch (result)
            {
                case MatchOutcome.HomeWin:
                    homeWins++;
                    break;
                case MatchOutcome.Draw:
                    draws++;
                    break;
                case MatchOutcome.AwayWin:
                    awayWins++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pastResults), result, "定義されていない結果が入っています。");
            }
        }

        double total = pastResults.Count;
        return Result.Success(MatchOutcomeProbabilities.FromValid(homeWins / total, draws / total, awayWins / total));
    }
}
