using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Forecasting;

/// <summary>
/// 係数の推定に使う、終わった試合1つ分のデータ。
/// レーティングは、その試合を反映する前の値（RatingChange の Before）を使う。
/// </summary>
public sealed record GoalObservation
{
    private GoalObservation(KickoffTime kickoff, double homeRatingBefore, double awayRatingBefore, Score score)
    {
        Kickoff = kickoff;
        HomeRatingBefore = homeRatingBefore;
        AwayRatingBefore = awayRatingBefore;
        Score = score;
    }

    public KickoffTime Kickoff { get; }

    public double HomeRatingBefore { get; }

    public double AwayRatingBefore { get; }

    /// <summary>90分のスコア。</summary>
    public Score Score { get; }

    /// <summary>Δ =（ホームの Elo − アウェイの Elo）÷ 100。</summary>
    public double RatingDifference => (HomeRatingBefore - AwayRatingBefore) / 100.0;

    public static Result<GoalObservation> Create(KickoffTime kickoff, double homeRatingBefore, double awayRatingBefore, Score score)
    {
        ArgumentNullException.ThrowIfNull(kickoff);
        ArgumentNullException.ThrowIfNull(score);
        return double.IsFinite(homeRatingBefore) && double.IsFinite(awayRatingBefore)
            ? Result.Success(new GoalObservation(kickoff, homeRatingBefore, awayRatingBefore, score))
            : Result.Failure<GoalObservation>(ForecastErrors.InvalidRating);
    }
}
