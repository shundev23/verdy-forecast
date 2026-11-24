using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Matches;

public class SeasonTests
{
    [Theory]
    [InlineData("2025")]
    [InlineData("1993")]
    [InlineData("2099")]
    [InlineData("2026-27")]
    [InlineData("1999-00")]
    [InlineData("2099-00")]
    public void Create_ValidFormat_Succeeds(string value) =>
        Season.Create(value).ShouldSucceed().Value.ShouldBe(value);

    [Fact]
    public void Create_WithSurroundingSpaces_TrimsValue() =>
        Season.Create(" 2026-27 ").ShouldSucceed().Value.ShouldBe("2026-27");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("25")]
    [InlineData("1992")]
    [InlineData("2100")]
    [InlineData("2026-28")]
    [InlineData("2026-26")]
    [InlineData("2026-2")]
    [InlineData("2026/27")]
    [InlineData("2026-2a")]
    [InlineData("2026-27-28")]
    [InlineData("20a6")]
    [InlineData("２０２５")]
    [InlineData("-2025")]
    public void Create_InvalidFormat_Fails(string? value) =>
        Season.Create(value).ShouldFailWith(MatchErrors.InvalidSeason);

    [Fact]
    public void ToString_ReturnsValue() => Make.SeasonOf("2026-27").ToString().ShouldBe("2026-27");
}
