using VerdyForecast.Domain.Players;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Players;

public class ShirtNumberTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void Create_InRange_Succeeds(int value) =>
        ShirtNumber.Create(value).ShouldSucceed().Value.ShouldBe(value);

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void Create_OutOfRange_Fails(int value) =>
        ShirtNumber.Create(value).ShouldFailWith(PlayerErrors.ShirtNumberOutOfRange);
}
