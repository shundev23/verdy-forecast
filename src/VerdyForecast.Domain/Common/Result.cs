using System.Diagnostics.CodeAnalysis;

namespace VerdyForecast.Domain.Common;

/// <summary>
/// 処理の結果。想定した失敗は例外ではなく、この型で返す（security.md の A10）。
/// 例外を使うのは、null を渡すなどのプログラムの誤りだけ。
/// </summary>
public class Result
{
    private protected Result(DomainError? error) => Error = error;

    /// <summary>失敗したときの内容。成功したときは null。</summary>
    public DomainError? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    public static Result Success() => new(null);

    public static Result Failure(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static Result<T> Success<T>(T value) => new(value);

    public static Result<T> Failure<T>(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(error);
    }
}

/// <summary>値を返す処理の結果。</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T value)
        : base(null) => _value = value;

    internal Result(DomainError error)
        : base(error)
    {
    }

    /// <summary>成功したときの値。失敗した結果から取り出そうとするのはプログラムの誤りなので、例外を出す。</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("失敗した結果から値は取り出せません。エラー: " + Error.Code);
}
