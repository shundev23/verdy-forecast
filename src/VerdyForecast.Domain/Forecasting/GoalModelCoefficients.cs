using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Forecasting;

/// <summary>
/// Elo の差から期待得点に直す係数（作業指示書 D5）。
/// log λ(ホーム) = a(ホーム) + b × Δ、log λ(アウェイ) = a(アウェイ) − b × Δ。Δ =（ホームの Elo − アウェイの Elo）÷ 100。
/// ホームの有利さは、a(ホーム) と a(アウェイ) の差に入る。
/// </summary>
public sealed record GoalModelCoefficients
{
    private GoalModelCoefficients(double homeIntercept, double awayIntercept, double slope)
    {
        HomeIntercept = homeIntercept;
        AwayIntercept = awayIntercept;
        Slope = slope;
    }

    /// <summary>a(ホーム)</summary>
    public double HomeIntercept { get; }

    /// <summary>a(アウェイ)</summary>
    public double AwayIntercept { get; }

    /// <summary>b</summary>
    public double Slope { get; }

    public static Result<GoalModelCoefficients> Create(double homeIntercept, double awayIntercept, double slope) =>
        double.IsFinite(homeIntercept) && double.IsFinite(awayIntercept) && double.IsFinite(slope)
            ? Result.Success(new GoalModelCoefficients(homeIntercept, awayIntercept, slope))
            : Result.Failure<GoalModelCoefficients>(ForecastErrors.InvalidCoefficients);

    /// <summary>試合前の Elo から、両チームの期待得点を出す。</summary>
    public Result<ExpectedGoals> ExpectedGoalsFor(double homeRating, double awayRating)
    {
        if (!double.IsFinite(homeRating) || !double.IsFinite(awayRating))
        {
            return Result.Failure<ExpectedGoals>(ForecastErrors.InvalidRating);
        }

        var difference = (homeRating - awayRating) / 100.0;
        return ExpectedGoals.Create(
            Math.Exp(HomeIntercept + (Slope * difference)),
            Math.Exp(AwayIntercept - (Slope * difference)));
    }

    // 推定の結果から作る（推定は有限の値しか返さない）
    internal static GoalModelCoefficients FromValid(double homeIntercept, double awayIntercept, double slope) =>
        new(homeIntercept, awayIntercept, slope);
}
