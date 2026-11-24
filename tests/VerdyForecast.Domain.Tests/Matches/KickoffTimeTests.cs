using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Tests.Matches;

public class KickoffTimeTests
{
    private static readonly DateTimeOffset _japanTime = new(2026, 11, 28, 14, 0, 0, TimeSpan.FromHours(9));
    private static readonly DateTimeOffset _sameInstantInUtc = new(2026, 11, 28, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void From_JapanTime_ConvertsToUtc()
    {
        var kickoff = KickoffTime.From(_japanTime);

        kickoff.Utc.ShouldBe(_sameInstantInUtc);
        kickoff.Utc.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void From_SameInstantWithDifferentOffsets_AreEqual() =>
        KickoffTime.From(_japanTime).ShouldBe(KickoffTime.From(_sameInstantInUtc));
}
