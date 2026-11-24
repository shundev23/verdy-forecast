using VerdyForecast.Domain.Reviews;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Reviews;

// 属性に decimal は書けないので、double で書いて decimal に直す
public class RatingScoreTests
{
    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(6.0)]
    [InlineData(9.5)]
    [InlineData(10.0)]
    public void Create_ValidValue_Succeeds(double value) =>
        RatingScore.Create((decimal)value).ShouldSucceed().Value.ShouldBe((decimal)value);

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.0)]
    [InlineData(10.5)]
    [InlineData(-6.0)]
    public void Create_OutOfRange_Fails(double value) =>
        RatingScore.Create((decimal)value).ShouldFailWith(ReviewErrors.RatingOutOfRange);

    [Theory]
    [InlineData(6.25)]
    [InlineData(6.1)]
    [InlineData(1.01)]
    [InlineData(9.99)]
    public void Create_InvalidStep_Fails(double value) =>
        RatingScore.Create((decimal)value).ShouldFailWith(ReviewErrors.RatingInvalidStep);
}
