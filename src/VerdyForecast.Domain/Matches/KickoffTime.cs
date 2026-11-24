namespace VerdyForecast.Domain.Matches;

/// <summary>
/// キックオフの日時。UTC に直して持つ（作業指示書の7章。画面で日本時間に直す）。
/// 予想のロックは、この日時で判定する（作業指示書 2-3）。
/// </summary>
public sealed record KickoffTime
{
    private KickoffTime(DateTimeOffset utc) => Utc = utc;

    /// <summary>キックオフの日時（UTC、オフセットは0）。</summary>
    public DateTimeOffset Utc { get; }

    /// <summary>どのオフセットの日時でも受け取り、同じ時点の UTC に直す。</summary>
    public static KickoffTime From(DateTimeOffset value) => new(value.ToUniversalTime());
}
