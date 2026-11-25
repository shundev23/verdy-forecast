using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Forecasting;

public class GoalModelCoefficientsTests
{
    private const double Tolerance = 1e-12;

    [Fact]
    public void Create_FiniteValues_Succeeds()
    {
        var coefficients = GoalModelCoefficients.Create(0.4, 0.1, 0.15).ShouldSucceed();

        coefficients.HomeIntercept.ShouldBe(0.4);
        coefficients.AwayIntercept.ShouldBe(0.1);
        coefficients.Slope.ShouldBe(0.15);
    }

    [Theory]
    [InlineData(double.NaN, 0.1, 0.15)]
    [InlineData(0.4, double.PositiveInfinity, 0.15)]
    [InlineData(0.4, 0.1, double.NegativeInfinity)]
    public void Create_NonFiniteValue_Fails(double homeIntercept, double awayIntercept, double slope) =>
        GoalModelCoefficients.Create(homeIntercept, awayIntercept, slope).ShouldFailWith(ForecastErrors.InvalidCoefficients);

    [Fact]
    public void ExpectedGoalsFor_EqualRatings_IsExpOfIntercepts()
    {
        var expected = Coefficients().ExpectedGoalsFor(1500, 1500).ShouldSucceed();

        expected.Home.ShouldBe(Math.Exp(0.4), Tolerance);
        expected.Away.ShouldBe(Math.Exp(0.1), Tolerance);
    }

    [Fact]
    public void ExpectedGoalsFor_HomeHundredPointsStronger_MovesBothBySlope()
    {
        var expected = Coefficients().ExpectedGoalsFor(1600, 1500).ShouldSucceed();

        expected.Home.ShouldBe(Math.Exp(0.4 + 0.15), Tolerance);
        expected.Away.ShouldBe(Math.Exp(0.1 - 0.15), Tolerance);
    }

    [Fact]
    public void ExpectedGoalsFor_AwayStronger_LowersHomeExpectedGoals()
    {
        var expected = Coefficients().ExpectedGoalsFor(1400, 1600).ShouldSucceed();

        expected.Home.ShouldBe(Math.Exp(0.4 - 0.3), Tolerance);
        expected.Away.ShouldBe(Math.Exp(0.1 + 0.3), Tolerance);
    }

    [Theory]
    [InlineData(double.NaN, 1500)]
    [InlineData(1500, double.PositiveInfinity)]
    public void ExpectedGoalsFor_NonFiniteRating_Fails(double homeRating, double awayRating) =>
        Coefficients().ExpectedGoalsFor(homeRating, awayRating).ShouldFailWith(ForecastErrors.InvalidRating);

    [Fact]
    public void ExpectedGoalsFor_TooLargeResult_FailsWithInvalidExpectedGoals() =>
        GoalModelCoefficients.Create(3.0, 0.1, 0.15).ShouldSucceed()
            .ExpectedGoalsFor(1500, 1500).ShouldFailWith(ForecastErrors.InvalidExpectedGoals);

    private static GoalModelCoefficients Coefficients() => GoalModelCoefficients.Create(0.4, 0.1, 0.15).ShouldSucceed();
}
