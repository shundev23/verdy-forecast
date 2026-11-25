using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Ratings;

/// <summary>
/// Elo の設定（作業指示書 D4）。実際の値は、過去のデータで選んで、モデルのバージョンとして DB に保存する。
/// </summary>
public sealed record EloSettings
{
    private EloSettings(double k, double homeAdvantage, double carryOver)
    {
        K = k;
        HomeAdvantage = homeAdvantage;
        CarryOver = carryOver;
    }

    /// <summary>既定値（K = 20、ホームの補正 = 50、c = 0.8）。値を選ぶ前や、テストで使う。</summary>
    public static EloSettings Default { get; } = new(k: 20, homeAdvantage: 50, carryOver: 0.8);

    /// <summary>1試合で動く大きさ。</summary>
    public double K { get; }

    /// <summary>ホームの補正（Elo の点）。Elo を更新するときの期待値の計算にだけ使う。</summary>
    public double HomeAdvantage { get; }

    /// <summary>シーズンの区切りで残す割合 c。R ← 平均 + c ×（R − 平均）。</summary>
    public double CarryOver { get; }

    public static Result<EloSettings> Create(double k, double homeAdvantage, double carryOver)
    {
        // NaN も弾くため、「範囲に入っている」の否定で書く
        if (!(k > 0 && k <= 100))
        {
            return Result.Failure<EloSettings>(RatingErrors.InvalidK);
        }

        if (!(homeAdvantage >= 0 && homeAdvantage <= 400))
        {
            return Result.Failure<EloSettings>(RatingErrors.InvalidHomeAdvantage);
        }

        if (!(carryOver >= 0 && carryOver <= 1))
        {
            return Result.Failure<EloSettings>(RatingErrors.InvalidCarryOver);
        }

        return Result.Success(new EloSettings(k, homeAdvantage, carryOver));
    }
}
