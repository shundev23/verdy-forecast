using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Ratings;

/// <summary>Elo レーティングの計算（作業指示書 D4）。</summary>
public static class Elo
{
    /// <summary>最初のシーズンの全チームの値。</summary>
    public const double InitialRating = 1500;

    /// <summary>ホームの期待勝ち点（勝ち = 1、引き分け = 0.5）。ホームの補正は、ここでだけ使う。</summary>
    public static double ExpectedHomeScore(double homeRating, double awayRating, double homeAdvantage) =>
        1.0 / (1.0 + Math.Pow(10, (awayRating - (homeRating + homeAdvantage)) / 400.0));

    /// <summary>得点差の補正。1点差以下は ×1、2点差は ×1.5、3点差以上は ×(11 + 点差) ÷ 8。</summary>
    public static double GoalDifferenceMultiplier(int goalDifference)
    {
        var difference = Math.Abs(goalDifference);
        return difference switch
        {
            <= 1 => 1.0,
            2 => 1.5,
            _ => (11.0 + difference) / 8.0,
        };
    }

    /// <summary>
    /// 1試合でホームのレーティングが動く量。アウェイは同じ量だけ逆向きに動く（2チームの合計は変わらない）。
    /// スコアは90分のもの（PK 戦の結果は含めない。作業指示書 D4）。
    /// </summary>
    public static double HomeRatingChange(double homeRating, double awayRating, Score score, EloSettings settings)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(settings);

        var expected = ExpectedHomeScore(homeRating, awayRating, settings.HomeAdvantage);
        var actual = score.Outcome switch
        {
            MatchOutcome.HomeWin => 1.0,
            MatchOutcome.Draw => 0.5,
            _ => 0.0,
        };

        return settings.K * GoalDifferenceMultiplier(score.Home - score.Away) * (actual - expected);
    }
}
