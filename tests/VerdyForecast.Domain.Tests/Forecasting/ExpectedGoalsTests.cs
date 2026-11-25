using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Forecasting;

public class ExpectedGoalsTests
{
    [Theory]
    [InlineData(1.5, 1.1)]
    [InlineData(0.01, 5.0)]
    public void Create_InRange_Succeeds(double home, double away)
    {
        var expected = ExpectedGoals.Create(home, away).ShouldSucceed();

        expected.Home.ShouldBe(home);
        expected.Away.ShouldBe(away);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(1.0, 0.0)]
    [InlineData(-1.0, 1.0)]
    [InlineData(5.0001, 1.0)]
    [InlineData(1.0, 5.0001)]
    [InlineData(double.NaN, 1.0)]
    [InlineData(1.0, double.PositiveInfinity)]
    public void Create_OutOfRange_Fails(double home, double away) =>
        ExpectedGoals.Create(home, away).ShouldFailWith(ForecastErrors.InvalidExpectedGoals);
}
