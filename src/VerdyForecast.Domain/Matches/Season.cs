using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Matches;

/// <summary>
/// シーズン。春秋制の年は「2025」、秋春制のシーズンは「2026-27」の形（作業指示書の7章）。
/// 2026年前半の百年構想リーグは「2026」。
/// </summary>
public sealed record Season
{
    public const int FirstYear = 1993;
    public const int LastYear = 2099;

    private Season(string value) => Value = value;

    public string Value { get; }

    public static Result<Season> Create(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        return IsCalendarYear(text) || IsSplitYear(text)
            ? Result.Success(new Season(text))
            : Result.Failure<Season>(MatchErrors.InvalidSeason);
    }

    public override string ToString() => Value;

    // 「2025」の形
    private static bool IsCalendarYear(string text) =>
        text.Length == 4 && TryParseYear(text.AsSpan(0, 4), out _);

    // 「2026-27」の形。後ろの2桁は、前の年の次の年
    private static bool IsSplitYear(string text) =>
        text.Length == 7
        && text[4] == '-'
        && TryParseYear(text.AsSpan(0, 4), out var startYear)
        && TryParseDigits(text.AsSpan(5, 2), out var nextYear)
        && nextYear == (startYear + 1) % 100;

    private static bool TryParseYear(ReadOnlySpan<char> digits, out int year) =>
        TryParseDigits(digits, out year) && year is >= FirstYear and <= LastYear;

    private static bool TryParseDigits(ReadOnlySpan<char> digits, out int number)
    {
        number = 0;
        foreach (var c in digits)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }

            number = (number * 10) + (c - '0');
        }

        return true;
    }
}
