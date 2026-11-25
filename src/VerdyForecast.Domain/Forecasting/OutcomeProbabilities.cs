using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Forecasting;

/// <summary>ホームから見た、勝ち・分け・負けの確率（J1 の全試合での評価はこの向き）。合計は 1。</summary>
public sealed record MatchOutcomeProbabilities
{
    private MatchOutcomeProbabilities(double homeWin, double draw, double awayWin)
    {
        HomeWin = homeWin;
        Draw = draw;
        AwayWin = awayWin;
    }

    public double HomeWin { get; }

    public double Draw { get; }

    public double AwayWin { get; }

    public static Result<MatchOutcomeProbabilities> Create(double homeWin, double draw, double awayWin) =>
        ProbabilityRules.AreValid(homeWin, draw, awayWin)
            ? Result.Success(new MatchOutcomeProbabilities(homeWin, draw, awayWin))
            : Result.Failure<MatchOutcomeProbabilities>(ForecastErrors.InvalidProbabilities);

    /// <summary>指定した側のチームから見た確率に直す（ヴェルディの試合の画面と、自分の予想との比較で使う）。</summary>
    public TeamOutcomeProbabilities ForTeam(Side side)
    {
        Guard.Defined(side);
        return side == Side.Home
            ? TeamOutcomeProbabilities.FromValid(HomeWin, Draw, AwayWin)
            : TeamOutcomeProbabilities.FromValid(AwayWin, Draw, HomeWin);
    }

    internal static MatchOutcomeProbabilities FromValid(double homeWin, double draw, double awayWin) =>
        new(homeWin, draw, awayWin);
}

/// <summary>あるチームから見た、勝ち・分け・負けの確率（自分の予想との比較はヴェルディから見たこの向き）。合計は 1。</summary>
public sealed record TeamOutcomeProbabilities
{
    private TeamOutcomeProbabilities(double win, double draw, double loss)
    {
        Win = win;
        Draw = draw;
        Loss = loss;
    }

    public double Win { get; }

    public double Draw { get; }

    public double Loss { get; }

    public static Result<TeamOutcomeProbabilities> Create(double win, double draw, double loss) =>
        ProbabilityRules.AreValid(win, draw, loss)
            ? Result.Success(new TeamOutcomeProbabilities(win, draw, loss))
            : Result.Failure<TeamOutcomeProbabilities>(ForecastErrors.InvalidProbabilities);

    internal static TeamOutcomeProbabilities FromValid(double win, double draw, double loss) => new(win, draw, loss);
}

/// <summary>3つの確率の条件（それぞれ 0〜1、合計が 1）。</summary>
internal static class ProbabilityRules
{
    // 浮動小数点の計算の誤差を許す幅
    public const double SumTolerance = 1e-9;

    public static bool AreValid(double first, double second, double third) =>
        IsProbability(first)
        && IsProbability(second)
        && IsProbability(third)
        && Math.Abs(first + second + third - 1.0) <= SumTolerance;

    // NaN も弾くため、「範囲に入っている」で書く
    private static bool IsProbability(double value) => value >= 0 && value <= 1;
}
