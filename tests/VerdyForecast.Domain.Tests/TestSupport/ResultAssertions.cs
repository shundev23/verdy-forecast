using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Tests.TestSupport;

/// <summary>結果（Result）の確認。失敗したときは、どのエラーだったかがメッセージに出る。</summary>
internal static class ResultAssertions
{
    public static T ShouldSucceed<T>(this Result<T> result)
    {
        result.Error.ShouldBeNull();
        return result.Value;
    }

    public static void ShouldSucceed(this Result result) => result.Error.ShouldBeNull();

    public static void ShouldFailWith(this Result result, DomainError expected)
    {
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(expected);
    }
}
