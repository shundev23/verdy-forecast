using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Forecasting;

/// <summary>
/// モデルの予想（作業指示書の7章の Forecasts）。作るのは <see cref="MatchForecaster"/> だけ。
/// 材料に使ったデータの時点（DataAsOf）は、必ずキックオフより前になる（データリーケージの防止）。
/// </summary>
public sealed class Forecast
{
    internal Forecast(
        MatchId matchId,
        Name modelVersionName,
        DateTimeOffset createdAt,
        DateTimeOffset dataAsOf,
        double homeRatingBefore,
        double awayRatingBefore,
        ExpectedGoals expectedGoals,
        ScoreDistribution distribution)
    {
        MatchId = matchId;
        ModelVersionName = modelVersionName;
        CreatedAt = createdAt;
        DataAsOf = dataAsOf;
        HomeRatingBefore = homeRatingBefore;
        AwayRatingBefore = awayRatingBefore;
        ExpectedGoals = expectedGoals;
        Distribution = distribution;
        MostLikelyScore = distribution.MostLikely(1)[0].Score;
    }

    public MatchId MatchId { get; }

    /// <summary>予想に使ったモデルのバージョンの名前。</summary>
    public Name ModelVersionName { get; }

    /// <summary>予想を作った日時（UTC）。</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// 材料に使ったデータの時点（UTC）。この日時より後に始まった試合は使っていない。
    /// 作った日時と、キックオフの3時間前の、早いほう（評価手順書 9.4 で、キックオフより前になっていることを確かめる）。
    /// </summary>
    public DateTimeOffset DataAsOf { get; }

    /// <summary>ホームの試合前の Elo。</summary>
    public double HomeRatingBefore { get; }

    /// <summary>アウェイの試合前の Elo。</summary>
    public double AwayRatingBefore { get; }

    public ExpectedGoals ExpectedGoals { get; }

    /// <summary>スコアの分布（各チーム0〜10点）。</summary>
    public ScoreDistribution Distribution { get; }

    /// <summary>ホームから見た、勝ち・分け・負けの確率（分布を足し合わせたもの）。</summary>
    public MatchOutcomeProbabilities Outcomes => Distribution.Outcomes;

    /// <summary>いちばん確率の高いスコア（答え合わせでの、モデルの予想スコア）。</summary>
    public Score MostLikelyScore { get; }
}
