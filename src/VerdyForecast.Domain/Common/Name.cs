namespace VerdyForecast.Domain.Common;

/// <summary>
/// チーム名、選手名、タグ名などの名前。前後の空白を除いて、1〜50文字。
/// </summary>
public sealed record Name
{
    public const int MaxLength = 50;

    private Name(string value) => Value = value;

    public string Value { get; }

    public static Result<Name> Create(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Result.Failure<Name>(CommonErrors.NameRequired);
        }

        if (trimmed.Length > MaxLength)
        {
            return Result.Failure<Name>(CommonErrors.NameTooLong);
        }

        return Result.Success(new Name(trimmed));
    }

    public override string ToString() => Value;
}
