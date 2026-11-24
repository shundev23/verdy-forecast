using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Matches;

public class TeamScoreTests
{
    [Fact]
    public void Create_ZeroZero_Succeeds()
    {
        var score = TeamScore.Create(0, 0).ShouldSucceed();

        score.GoalsFor.ShouldBe(0);
        score.GoalsAgainst.ShouldBe(0);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Create_NegativeGoals_Fails(int goalsFor, int goalsAgainst) =>
        TeamScore.Create(goalsFor, goalsAgainst).ShouldFailWith(MatchErrors.NegativeGoals);

    [Theory]
    [InlineData(2, 1, TeamOutcome.Win)]
    [InlineData(1, 1, TeamOutcome.Draw)]
    [InlineData(0, 0, TeamOutcome.Draw)]
    [InlineData(0, 1, TeamOutcome.Loss)]
    public void Outcome_FollowsGoals(int goalsFor, int goalsAgainst, TeamOutcome expected) =>
        Make.TeamScoreOf(goalsFor, goalsAgainst).Outcome.ShouldBe(expected);

    [Fact]
    public void ToScore_HomeSide_KeepsOrder() => Make.TeamScoreOf(2, 1).ToScore(Side.Home).ShouldBe(Make.ScoreOf(2, 1));

    [Fact]
    public void ToScore_AwaySide_SwapsGoals() => Make.TeamScoreOf(2, 1).ToScore(Side.Away).ShouldBe(Make.ScoreOf(1, 2));

    [Theory]
    [InlineData(Side.Home)]
    [InlineData(Side.Away)]
    public void ToScore_AfterToTeamScore_ReturnsOriginal(Side side)
    {
        var original = Make.ScoreOf(3, 1);

        original.ToTeamScore(side).ToScore(side).ShouldBe(original);
    }

    [Fact]
    public void ToScore_UndefinedSide_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Make.TeamScoreOf(2, 1).ToScore((Side)99));
}
