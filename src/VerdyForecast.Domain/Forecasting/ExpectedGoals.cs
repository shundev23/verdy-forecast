using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Forecasting;

/// <summary>
/// 期待得点（ポアソン分布の λ）。0 より大きく 5 以下。
/// スコアの分布は各チーム0〜10点で計算するので、λ が大きすぎると、10点より上の確率を切り捨てる影響が大きくなる。
/// J1 の期待得点はふつう 3 を超えないので、5 を超えたら係数の誤りとして扱う。
/// </summary>
public sealed record ExpectedGoals
{
    public const double Max = 5.0;

    private ExpectedGoals(double home, double away)
    {
        Home = home;
        Away = away;
    }

    public double Home { get; }

    public double Away { get; }

    public static Result<ExpectedGoals> Create(double home, double away) =>
        IsValid(home) && IsValid(away)
            ? Result.Success(new ExpectedGoals(home, away))
            : Result.Failure<ExpectedGoals>(ForecastErrors.InvalidExpectedGoals);

    // NaN も弾くため、「範囲に入っている」で書く
    private static bool IsValid(double value) => value > 0 && value <= Max;
}
