using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Tests.Matches;

public class OutcomeConversionsTests
{
    // ホームから見た結果、チームの側、そのチームから見た結果
    public static TheoryData<MatchOutcome, Side, TeamOutcome> Conversions => new()
    {
        { MatchOutcome.HomeWin, Side.Home, TeamOutcome.Win },
        { MatchOutcome.HomeWin, Side.Away, TeamOutcome.Loss },
        { MatchOutcome.Draw, Side.Home, TeamOutcome.Draw },
        { MatchOutcome.Draw, Side.Away, TeamOutcome.Draw },
        { MatchOutcome.AwayWin, Side.Home, TeamOutcome.Loss },
        { MatchOutcome.AwayWin, Side.Away, TeamOutcome.Win },
    };

    [Theory]
    [MemberData(nameof(Conversions))]
    public void ToTeamOutcome_ConvertsBySide(MatchOutcome matchOutcome, Side side, TeamOutcome teamOutcome) =>
        matchOutcome.ToTeamOutcome(side).ShouldBe(teamOutcome);

    [Theory]
    [MemberData(nameof(Conversions))]
    public void ToMatchOutcome_ConvertsBySide(MatchOutcome matchOutcome, Side side, TeamOutcome teamOutcome) =>
        teamOutcome.ToMatchOutcome(side).ShouldBe(matchOutcome);

    [Fact]
    public void ToTeamOutcome_UndefinedValue_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ((MatchOutcome)99).ToTeamOutcome(Side.Home));
        Should.Throw<ArgumentOutOfRangeException>(() => MatchOutcome.Draw.ToTeamOutcome((Side)99));
    }

    [Fact]
    public void ToMatchOutcome_UndefinedValue_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ((TeamOutcome)99).ToMatchOutcome(Side.Home));
        Should.Throw<ArgumentOutOfRangeException>(() => TeamOutcome.Draw.ToMatchOutcome((Side)99));
    }
}
