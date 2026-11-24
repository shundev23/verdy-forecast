using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Players;

/// <summary>背番号（1〜99）。</summary>
public sealed record ShirtNumber
{
    public const int Min = 1;
    public const int Max = 99;

    private ShirtNumber(int value) => Value = value;

    public int Value { get; }

    public static Result<ShirtNumber> Create(int value) =>
        value is < Min or > Max
            ? Result.Failure<ShirtNumber>(PlayerErrors.ShirtNumberOutOfRange)
            : Result.Success(new ShirtNumber(value));
}
