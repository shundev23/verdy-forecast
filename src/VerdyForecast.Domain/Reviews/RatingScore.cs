using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Reviews;

/// <summary>採点（1.0〜10.0 の 0.5 刻み。6.0 が及第点。作業指示書 D6）。</summary>
public sealed record RatingScore
{
    public const decimal Min = 1.0m;
    public const decimal Max = 10.0m;
    public const decimal Step = 0.5m;

    private RatingScore(decimal value) => Value = value;

    public decimal Value { get; }

    public static Result<RatingScore> Create(decimal value)
    {
        if (value is < Min or > Max)
        {
            return Result.Failure<RatingScore>(ReviewErrors.RatingOutOfRange);
        }

        if (value % Step != 0m)
        {
            return Result.Failure<RatingScore>(ReviewErrors.RatingInvalidStep);
        }

        return Result.Success(new RatingScore(value));
    }

    // 範囲と刻みを満たしていると分かっている値から作る（参考点の計算だけで使う）
    internal static RatingScore FromValid(decimal value) => new(value);
}
