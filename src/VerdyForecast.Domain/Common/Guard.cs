using System.Runtime.CompilerServices;

namespace VerdyForecast.Domain.Common;

/// <summary>プログラムの誤り（定義されていない列挙値など）を見つけて、例外を出す。</summary>
internal static class Guard
{
    public static void Defined<TEnum>(TEnum value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, "定義されていない値です。");
        }
    }
}
