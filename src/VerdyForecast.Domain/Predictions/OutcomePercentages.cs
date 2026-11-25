using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Predictions;

/// <summary>
/// 自分の予想の確率（％。ヴェルディから見た勝ち・分け・負け）。
/// 合計100、どれも1以上（作業指示書 D8。0%だと対数損失が無限大になるため）。
/// </summary>
public sealed record OutcomePercentages
{
    public const int Minimum = 1;
    public const int Total = 100;

    private OutcomePercentages(int win, int draw, int loss)
    {
        Win = win;
        Draw = draw;
        Loss = loss;
    }

    public int Win { get; }

    public int Draw { get; }

    public int Loss { get; }

    public static Result<OutcomePercentages> Create(int win, int draw, int loss)
    {
        if (win < Minimum || draw < Minimum || loss < Minimum)
        {
            return Result.Failure<OutcomePercentages>(PredictionErrors.PercentageBelowMinimum);
        }

        // 大きな値どうしの足し算で int があふれて、たまたま100になることがないよう、long で足す
        if ((long)win + draw + loss != Total)
        {
            return Result.Failure<OutcomePercentages>(PredictionErrors.PercentagesNotTotal);
        }

        return Result.Success(new OutcomePercentages(win, draw, loss));
    }

    /// <summary>指定した結果の％。</summary>
    public int Of(TeamOutcome outcome) => outcome switch
    {
        TeamOutcome.Win => Win,
        TeamOutcome.Draw => Draw,
        TeamOutcome.Loss => Loss,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "定義されていない結果です。"),
    };

    /// <summary>指定した結果の％が、ほかのどれよりも低くないか（同じ値は可）。</summary>
    public bool IsHighest(TeamOutcome outcome)
    {
        var percentage = Of(outcome);
        return percentage >= Win && percentage >= Draw && percentage >= Loss;
    }

    // 条件を満たしていると分かっている値から作る（D8 のルールの表だけで使う）
    internal static OutcomePercentages FromValid(int win, int draw, int loss) => new(win, draw, loss);
}
