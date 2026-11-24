using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Matches;

public class ScoreTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 2)]
    public void Create_NonNegativeGoals_Succeeds(int home, int away)
    {
        var score = Score.Create(home, away).ShouldSucceed();

        score.Home.ShouldBe(home);
        score.Away.ShouldBe(away);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Create_NegativeGoals_Fails(int home, int away) =>
        Score.Create(home, away).ShouldFailWith(MatchErrors.NegativeGoals);

    [Theory]
    [InlineData(2, 1, MatchOutcome.HomeWin)]
    [InlineData(1, 1, MatchOutcome.Draw)]
    [InlineData(0, 0, MatchOutcome.Draw)]
    [InlineData(0, 1, MatchOutcome.AwayWin)]
    public void Outcome_FollowsGoals(int home, int away, MatchOutcome expected) =>
        Make.ScoreOf(home, away).Outcome.ShouldBe(expected);

    [Fact]
    public void ToTeamScore_HomeSide_KeepsOrder()
    {
        var teamScore = Make.ScoreOf(2, 1).ToTeamScore(Side.Home);

        teamScore.GoalsFor.ShouldBe(2);
        teamScore.GoalsAgainst.ShouldBe(1);
    }

    [Fact]
    public void ToTeamScore_AwaySide_SwapsGoals()
    {
        var teamScore = Make.ScoreOf(2, 1).ToTeamScore(Side.Away);

        teamScore.GoalsFor.ShouldBe(1);
        teamScore.GoalsAgainst.ShouldBe(2);
    }

    [Fact]
    public void ToTeamScore_UndefinedSide_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Make.ScoreOf(2, 1).ToTeamScore((Side)99));
}
