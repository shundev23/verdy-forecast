using VerdyForecast.Domain.Ratings;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Ratings;

public class EloSettingsTests
{
    [Fact]
    public void Default_IsD4DefaultValues()
    {
        EloSettings.Default.K.ShouldBe(20);
        EloSettings.Default.HomeAdvantage.ShouldBe(50);
        EloSettings.Default.CarryOver.ShouldBe(0.8);
    }

    [Theory]
    [InlineData(100, 400, 1)]
    [InlineData(0.1, 0, 0)]
    public void Create_BoundaryValues_Succeeds(double k, double homeAdvantage, double carryOver)
    {
        var settings = EloSettings.Create(k, homeAdvantage, carryOver).ShouldSucceed();

        settings.K.ShouldBe(k);
        settings.HomeAdvantage.ShouldBe(homeAdvantage);
        settings.CarryOver.ShouldBe(carryOver);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100.1)]
    [InlineData(double.NaN)]
    public void Create_InvalidK_Fails(double k) =>
        EloSettings.Create(k, 50, 0.8).ShouldFailWith(RatingErrors.InvalidK);

    [Theory]
    [InlineData(-1)]
    [InlineData(400.1)]
    [InlineData(double.NaN)]
    public void Create_InvalidHomeAdvantage_Fails(double homeAdvantage) =>
        EloSettings.Create(20, homeAdvantage, 0.8).ShouldFailWith(RatingErrors.InvalidHomeAdvantage);

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Create_InvalidCarryOver_Fails(double carryOver) =>
        EloSettings.Create(20, 50, carryOver).ShouldFailWith(RatingErrors.InvalidCarryOver);
}
