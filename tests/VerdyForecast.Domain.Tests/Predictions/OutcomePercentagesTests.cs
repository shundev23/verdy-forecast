using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Predictions;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Predictions;

public class OutcomePercentagesTests
{
    [Theory]
    [InlineData(60, 25, 15)]
    [InlineData(34, 33, 33)]
    [InlineData(1, 1, 98)]
    [InlineData(98, 1, 1)]
    public void Create_ValidPercentages_Succeeds(int win, int draw, int loss)
    {
        var percentages = OutcomePercentages.Create(win, draw, loss).ShouldSucceed();

        percentages.Win.ShouldBe(win);
        percentages.Draw.ShouldBe(draw);
        percentages.Loss.ShouldBe(loss);
    }

    [Theory]
    [InlineData(0, 50, 50)]
    [InlineData(50, 0, 50)]
    [InlineData(50, 50, 0)]
    [InlineData(-1, 51, 50)]
    public void Create_BelowMinimum_Fails(int win, int draw, int loss) =>
        OutcomePercentages.Create(win, draw, loss).ShouldFailWith(PredictionErrors.PercentageBelowMinimum);

    [Theory]
    [InlineData(60, 25, 14)]
    [InlineData(60, 25, 16)]
    public void Create_SumNot100_Fails(int win, int draw, int loss) =>
        OutcomePercentages.Create(win, draw, loss).ShouldFailWith(PredictionErrors.PercentagesNotTotal);

    [Fact]
    public void Create_ValuesThatOverflowToExactly100_Fails() =>
        OutcomePercentages.Create(int.MaxValue, int.MaxValue, 102).ShouldFailWith(PredictionErrors.PercentagesNotTotal);

    [Theory]
    [InlineData(TeamOutcome.Win, 60)]
    [InlineData(TeamOutcome.Draw, 25)]
    [InlineData(TeamOutcome.Loss, 15)]
    public void Of_ReturnsPercentageOfOutcome(TeamOutcome outcome, int expected) =>
        Make.PercentagesOf(60, 25, 15).Of(outcome).ShouldBe(expected);

    [Theory]
    [InlineData(40, 40, 20, TeamOutcome.Win, true)]
    [InlineData(40, 40, 20, TeamOutcome.Draw, true)]
    [InlineData(40, 40, 20, TeamOutcome.Loss, false)]
    [InlineData(20, 30, 50, TeamOutcome.Loss, true)]
    [InlineData(20, 30, 50, TeamOutcome.Draw, false)]
    [InlineData(20, 30, 50, TeamOutcome.Win, false)]
    public void IsHighest_AllowsTies(int win, int draw, int loss, TeamOutcome outcome, bool expected) =>
        Make.PercentagesOf(win, draw, loss).IsHighest(outcome).ShouldBe(expected);

    [Fact]
    public void Of_UndefinedOutcome_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Make.PercentagesOf(60, 25, 15).Of((TeamOutcome)99));
}
