using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Tests.Common;

public class ResultTests
{
    private static readonly DomainError _sampleError = new("sample.error", "テスト用のエラー");

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_HasError()
    {
        var result = Result.Failure(_sampleError);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(_sampleError);
    }

    [Fact]
    public void SuccessWithValue_ReturnsValue() => Result.Success(42).Value.ShouldBe(42);

    [Fact]
    public void FailureWithValue_HasError()
    {
        var result = Result.Failure<int>(_sampleError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(_sampleError);
    }

    [Fact]
    public void Value_OfFailure_ThrowsWithErrorCode()
    {
        var result = Result.Failure<int>(_sampleError);

        Should.Throw<InvalidOperationException>(() => result.Value).Message.ShouldContain("sample.error");
    }

    [Fact]
    public void Failure_NullError_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
        Should.Throw<ArgumentNullException>(() => Result.Failure<int>(null!));
    }
}
