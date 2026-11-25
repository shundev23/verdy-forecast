using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Predictions;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Predictions;

public class PercentageConversionTests
{
    [Theory]
    [InlineData(TeamOutcome.Win, 60, 25, 15)]
    [InlineData(TeamOutcome.Draw, 20, 60, 20)]
    [InlineData(TeamOutcome.Loss, 15, 25, 60)]
    public void FromChoiceV1_ReturnsRuleV1Percentages(TeamOutcome choice, int win, int draw, int loss) =>
        PercentageConversion.FromChoiceV1(choice).ShouldBe(Make.PercentagesOf(win, draw, loss));

    [Fact]
    public void FromChoiceV1_UndefinedChoice_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => PercentageConversion.FromChoiceV1((TeamOutcome)99));
}
