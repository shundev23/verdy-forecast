using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Predictions;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Predictions;

public class MyPredictionTests
{
    private static readonly MatchId _matchId = new(10);
    private static readonly DateTimeOffset _enteredAt = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(TeamOutcome.Win, 2, 1)]
    [InlineData(TeamOutcome.Draw, 0, 0)]
    [InlineData(TeamOutcome.Draw, 2, 2)]
    [InlineData(TeamOutcome.Loss, 0, 1)]
    public void Create_ScoreMatchesChoice_Succeeds(TeamOutcome choice, int goalsFor, int goalsAgainst)
    {
        var prediction = MyPrediction.Create(_matchId, choice, Make.TeamScoreOf(goalsFor, goalsAgainst), null, _enteredAt).ShouldSucceed();

        prediction.MatchId.ShouldBe(_matchId);
        prediction.Choice.ShouldBe(choice);
        prediction.PredictedScore.ShouldBe(Make.TeamScoreOf(goalsFor, goalsAgainst));
        prediction.Percentages.ShouldBeNull();
        prediction.EnteredAt.ShouldBe(_enteredAt);
    }

    [Theory]
    [InlineData(TeamOutcome.Win, 1, 1)]
    [InlineData(TeamOutcome.Win, 0, 1)]
    [InlineData(TeamOutcome.Draw, 2, 1)]
    [InlineData(TeamOutcome.Loss, 1, 0)]
    public void Create_ScoreDoesNotMatchChoice_Fails(TeamOutcome choice, int goalsFor, int goalsAgainst) =>
        MyPrediction.Create(_matchId, choice, Make.TeamScoreOf(goalsFor, goalsAgainst), null, _enteredAt)
            .ShouldFailWith(PredictionErrors.ScoreDoesNotMatchChoice);

    [Fact]
    public void Create_ChoiceHasHighestPercentage_StoresPercentages()
    {
        var prediction = MyPrediction.Create(_matchId, TeamOutcome.Win, Make.TeamScoreOf(2, 1), Make.PercentagesOf(60, 25, 15), _enteredAt).ShouldSucceed();

        prediction.Percentages.ShouldBe(Make.PercentagesOf(60, 25, 15));
    }

    [Fact]
    public void Create_ChoiceTiedForHighestPercentage_Succeeds() =>
        MyPrediction.Create(_matchId, TeamOutcome.Win, Make.TeamScoreOf(1, 0), Make.PercentagesOf(40, 40, 20), _enteredAt).ShouldSucceed();

    [Theory]
    [InlineData(TeamOutcome.Win, 2, 1, 30, 50, 20)]
    [InlineData(TeamOutcome.Loss, 0, 2, 40, 30, 30)]
    public void Create_ChoiceNotHighestPercentage_Fails(TeamOutcome choice, int goalsFor, int goalsAgainst, int win, int draw, int loss) =>
        MyPrediction.Create(_matchId, choice, Make.TeamScoreOf(goalsFor, goalsAgainst), Make.PercentagesOf(win, draw, loss), _enteredAt)
            .ShouldFailWith(PredictionErrors.ChoiceNotHighest);

    [Fact]
    public void Create_JapanTime_StoresEnteredAtAsUtc()
    {
        var japanTime = new DateTimeOffset(2026, 11, 27, 21, 0, 0, TimeSpan.FromHours(9));

        var prediction = MyPrediction.Create(_matchId, TeamOutcome.Draw, Make.TeamScoreOf(1, 1), null, japanTime).ShouldSucceed();

        prediction.EnteredAt.ShouldBe(_enteredAt);
        prediction.EnteredAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Create_UndefinedChoice_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => MyPrediction.Create(_matchId, (TeamOutcome)99, Make.TeamScoreOf(1, 0), null, _enteredAt));
}
