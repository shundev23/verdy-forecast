using Microsoft.Extensions.Time.Testing;
using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Predictions;
using VerdyForecast.Domain.Teams;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Predictions;

public class MyPredictionTests
{
    private const int MatchIdValue = 10;
    private static readonly TeamId _verdy = new(1);
    private static readonly TeamId _opponent = new(2);
    private static readonly TeamId _otherTeam = new(3);

    // ---- 作る ----

    [Fact]
    public void Create_OneSecondBeforeKickoff_Succeeds()
    {
        var match = VerdyHomeMatch();
        var clock = ClockAt(match, secondsFromKickoff: -1);

        var prediction = MyPrediction.Create(match, _verdy, TeamOutcome.Win, Make.TeamScoreOf(2, 1), null, clock).ShouldSucceed();

        prediction.MatchId.ShouldBe(new MatchId(MatchIdValue));
        prediction.Choice.ShouldBe(TeamOutcome.Win);
        prediction.PredictedScore.ShouldBe(Make.TeamScoreOf(2, 1));
        prediction.Percentages.ShouldBeNull();
        prediction.EnteredAt.ShouldBe(clock.GetUtcNow());
        prediction.UpdatedAt.ShouldBe(clock.GetUtcNow());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Create_AtOrAfterKickoff_FailsWithLocked(int secondsFromKickoff)
    {
        var match = VerdyHomeMatch();

        CreateWin(match, ClockAt(match, secondsFromKickoff)).ShouldFailWith(PredictionErrors.Locked);
    }

    [Fact]
    public void Create_PostponedMatch_FailsWithLocked()
    {
        var match = VerdyHomeMatch();
        match.Postpone().ShouldSucceed();

        CreateWin(match, ClockAt(match, secondsFromKickoff: -86_400)).ShouldFailWith(PredictionErrors.Locked);
    }

    [Fact]
    public void Create_RescheduledMatchBeforeNewKickoff_Succeeds()
    {
        var match = VerdyHomeMatch();
        var clockAfterOriginalKickoff = ClockAt(match, secondsFromKickoff: 3_600);
        match.Postpone().ShouldSucceed();
        match.Reschedule(Make.KickoffAt(day: 30, hour: 10)).ShouldSucceed();

        CreateWin(match, clockAfterOriginalKickoff).ShouldSucceed();
    }

    [Fact]
    public void Create_FinishedMatch_FailsWithLocked()
    {
        var match = VerdyHomeMatch();
        match.RecordResult(Make.ScoreOf(1, 0)).ShouldSucceed();

        CreateWin(match, ClockAt(match, secondsFromKickoff: -60)).ShouldFailWith(PredictionErrors.Locked);
    }

    [Fact]
    public void Create_VerdyIsAwayTeam_Succeeds()
    {
        var match = Make.ScheduledMatch(_opponent, _verdy).WithId(MatchIdValue);

        CreateWin(match, ClockAt(match, secondsFromKickoff: -1)).ShouldSucceed();
    }

    [Fact]
    public void Create_MatchWithoutVerdy_FailsWithNotVerdyMatch()
    {
        var match = Make.ScheduledMatch(_opponent, _otherTeam).WithId(MatchIdValue);

        CreateWin(match, ClockAt(match, secondsFromKickoff: -1)).ShouldFailWith(PredictionErrors.NotVerdyMatch);
    }

    [Fact]
    public void Create_AfterKickoffWithInvalidContent_FailsWithLocked()
    {
        var match = VerdyHomeMatch();

        MyPrediction.Create(match, _verdy, TeamOutcome.Win, Make.TeamScoreOf(1, 1), null, ClockAt(match, secondsFromKickoff: 0))
            .ShouldFailWith(PredictionErrors.Locked);
    }

    [Theory]
    [InlineData(TeamOutcome.Win, 2, 1)]
    [InlineData(TeamOutcome.Draw, 0, 0)]
    [InlineData(TeamOutcome.Draw, 2, 2)]
    [InlineData(TeamOutcome.Loss, 0, 1)]
    public void Create_ScoreMatchesChoice_Succeeds(TeamOutcome choice, int goalsFor, int goalsAgainst)
    {
        var match = VerdyHomeMatch();

        MyPrediction.Create(match, _verdy, choice, Make.TeamScoreOf(goalsFor, goalsAgainst), null, ClockAt(match, -1)).ShouldSucceed();
    }

    [Theory]
    [InlineData(TeamOutcome.Win, 1, 1)]
    [InlineData(TeamOutcome.Win, 0, 1)]
    [InlineData(TeamOutcome.Draw, 2, 1)]
    [InlineData(TeamOutcome.Loss, 1, 0)]
    public void Create_ScoreDoesNotMatchChoice_Fails(TeamOutcome choice, int goalsFor, int goalsAgainst)
    {
        var match = VerdyHomeMatch();

        MyPrediction.Create(match, _verdy, choice, Make.TeamScoreOf(goalsFor, goalsAgainst), null, ClockAt(match, -1))
            .ShouldFailWith(PredictionErrors.ScoreDoesNotMatchChoice);
    }

    [Fact]
    public void Create_ChoiceTiedForHighestPercentage_Succeeds()
    {
        var match = VerdyHomeMatch();

        MyPrediction.Create(match, _verdy, TeamOutcome.Win, Make.TeamScoreOf(1, 0), Make.PercentagesOf(40, 40, 20), ClockAt(match, -1))
            .ShouldSucceed();
    }

    [Theory]
    [InlineData(TeamOutcome.Win, 2, 1, 30, 50, 20)]
    [InlineData(TeamOutcome.Loss, 0, 2, 40, 30, 30)]
    public void Create_ChoiceNotHighestPercentage_Fails(TeamOutcome choice, int goalsFor, int goalsAgainst, int win, int draw, int loss)
    {
        var match = VerdyHomeMatch();

        MyPrediction.Create(match, _verdy, choice, Make.TeamScoreOf(goalsFor, goalsAgainst), Make.PercentagesOf(win, draw, loss), ClockAt(match, -1))
            .ShouldFailWith(PredictionErrors.ChoiceNotHighest);
    }

    [Fact]
    public void Create_UndefinedChoice_Throws()
    {
        var match = VerdyHomeMatch();

        Should.Throw<ArgumentOutOfRangeException>(() =>
            MyPrediction.Create(match, _verdy, (TeamOutcome)99, Make.TeamScoreOf(1, 0), null, ClockAt(match, -1)));
    }

    // ---- 評価に使う確率（作業指示書 D8） ----

    [Theory]
    [InlineData(TeamOutcome.Win, 2, 1, 60, 25, 15)]
    [InlineData(TeamOutcome.Draw, 1, 1, 20, 60, 20)]
    [InlineData(TeamOutcome.Loss, 0, 1, 15, 25, 60)]
    public void Create_WithoutPercentages_ConvertsByRuleV1(TeamOutcome choice, int goalsFor, int goalsAgainst, int win, int draw, int loss)
    {
        var match = VerdyHomeMatch();

        var prediction = MyPrediction.Create(match, _verdy, choice, Make.TeamScoreOf(goalsFor, goalsAgainst), null, ClockAt(match, -1)).ShouldSucceed();

        prediction.EvaluationPercentages.ShouldBe(Make.PercentagesOf(win, draw, loss));
        prediction.EvaluationSource.ShouldBe(EvaluationPercentagesSource.ConvertedByRuleV1);
    }

    [Fact]
    public void Create_WithPercentages_UsesEnteredValuesForEvaluation()
    {
        var match = VerdyHomeMatch();

        var prediction = MyPrediction.Create(match, _verdy, TeamOutcome.Win, Make.TeamScoreOf(2, 1), Make.PercentagesOf(70, 20, 10), ClockAt(match, -1))
            .ShouldSucceed();

        prediction.Percentages.ShouldBe(Make.PercentagesOf(70, 20, 10));
        prediction.EvaluationPercentages.ShouldBe(Make.PercentagesOf(70, 20, 10));
        prediction.EvaluationSource.ShouldBe(EvaluationPercentagesSource.Entered);
    }

    // ---- 変える ----

    [Fact]
    public void Change_OneSecondBeforeKickoff_UpdatesValuesAndKeepsEnteredAt()
    {
        var match = VerdyHomeMatch();
        var clock = ClockAt(match, secondsFromKickoff: -3_600);
        var prediction = CreateWin(match, clock).ShouldSucceed();
        var enteredAt = prediction.EnteredAt;
        clock.SetUtcNow(match.Kickoff.Utc.AddSeconds(-1));

        prediction.Change(match, TeamOutcome.Draw, Make.TeamScoreOf(1, 1), Make.PercentagesOf(30, 40, 30), clock).ShouldSucceed();

        prediction.Choice.ShouldBe(TeamOutcome.Draw);
        prediction.PredictedScore.ShouldBe(Make.TeamScoreOf(1, 1));
        prediction.Percentages.ShouldBe(Make.PercentagesOf(30, 40, 30));
        prediction.EvaluationPercentages.ShouldBe(Make.PercentagesOf(30, 40, 30));
        prediction.EvaluationSource.ShouldBe(EvaluationPercentagesSource.Entered);
        prediction.EnteredAt.ShouldBe(enteredAt);
        prediction.UpdatedAt.ShouldBe(match.Kickoff.Utc.AddSeconds(-1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Change_AtOrAfterKickoff_FailsAndKeepsValues(int secondsFromKickoff)
    {
        var match = VerdyHomeMatch();
        var clock = ClockAt(match, secondsFromKickoff: -60);
        var prediction = CreateWin(match, clock).ShouldSucceed();
        clock.SetUtcNow(match.Kickoff.Utc.AddSeconds(secondsFromKickoff));

        prediction.Change(match, TeamOutcome.Loss, Make.TeamScoreOf(0, 3), null, clock).ShouldFailWith(PredictionErrors.Locked);

        ShouldBeUnchangedWin(prediction, updatedAt: match.Kickoff.Utc.AddSeconds(-60));
    }

    [Fact]
    public void Change_PostponedMatch_FailsWithLocked()
    {
        var match = VerdyHomeMatch();
        var clock = ClockAt(match, secondsFromKickoff: -60);
        var prediction = CreateWin(match, clock).ShouldSucceed();
        match.Postpone().ShouldSucceed();

        prediction.Change(match, TeamOutcome.Loss, Make.TeamScoreOf(0, 3), null, clock).ShouldFailWith(PredictionErrors.Locked);
    }

    [Fact]
    public void Change_InvalidContent_FailsAndKeepsValues()
    {
        var match = VerdyHomeMatch();
        var clock = ClockAt(match, secondsFromKickoff: -60);
        var prediction = CreateWin(match, clock).ShouldSucceed();
        clock.Advance(TimeSpan.FromSeconds(30));

        prediction.Change(match, TeamOutcome.Win, Make.TeamScoreOf(1, 1), null, clock).ShouldFailWith(PredictionErrors.ScoreDoesNotMatchChoice);

        ShouldBeUnchangedWin(prediction, updatedAt: match.Kickoff.Utc.AddSeconds(-60));
    }

    [Fact]
    public void Change_RemovingPercentages_ConvertsByRuleV1()
    {
        var match = VerdyHomeMatch();
        var clock = ClockAt(match, secondsFromKickoff: -60);
        var prediction = MyPrediction.Create(match, _verdy, TeamOutcome.Win, Make.TeamScoreOf(2, 1), Make.PercentagesOf(70, 20, 10), clock).ShouldSucceed();

        prediction.Change(match, TeamOutcome.Draw, Make.TeamScoreOf(1, 1), null, clock).ShouldSucceed();

        prediction.Percentages.ShouldBeNull();
        prediction.EvaluationPercentages.ShouldBe(Make.PercentagesOf(20, 60, 20));
        prediction.EvaluationSource.ShouldBe(EvaluationPercentagesSource.ConvertedByRuleV1);
    }

    [Fact]
    public void Change_DifferentMatch_Throws()
    {
        var match = VerdyHomeMatch();
        var clock = ClockAt(match, secondsFromKickoff: -60);
        var prediction = CreateWin(match, clock).ShouldSucceed();
        var otherMatch = Make.ScheduledMatch(_verdy, _opponent).WithId(MatchIdValue + 1);

        Should.Throw<ArgumentException>(() => prediction.Change(otherMatch, TeamOutcome.Win, Make.TeamScoreOf(2, 1), null, clock));
    }

    // ---- 消す ----

    [Fact]
    public void CheckCanDelete_OneSecondBeforeKickoff_Succeeds()
    {
        var match = VerdyHomeMatch();
        var clock = ClockAt(match, secondsFromKickoff: -60);
        var prediction = CreateWin(match, clock).ShouldSucceed();
        clock.SetUtcNow(match.Kickoff.Utc.AddSeconds(-1));

        prediction.CheckCanDelete(match, clock).ShouldSucceed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CheckCanDelete_AtOrAfterKickoff_FailsWithLocked(int secondsFromKickoff)
    {
        var match = VerdyHomeMatch();
        var clock = ClockAt(match, secondsFromKickoff: -60);
        var prediction = CreateWin(match, clock).ShouldSucceed();
        clock.SetUtcNow(match.Kickoff.Utc.AddSeconds(secondsFromKickoff));

        prediction.CheckCanDelete(match, clock).ShouldFailWith(PredictionErrors.Locked);
    }

    [Fact]
    public void CheckCanDelete_DifferentMatch_Throws()
    {
        var match = VerdyHomeMatch();
        var clock = ClockAt(match, secondsFromKickoff: -60);
        var prediction = CreateWin(match, clock).ShouldSucceed();
        var otherMatch = Make.ScheduledMatch(_verdy, _opponent).WithId(MatchIdValue + 1);

        Should.Throw<ArgumentException>(() => prediction.CheckCanDelete(otherMatch, clock));
    }

    // ---- テスト用の補助 ----

    // 試合の状態を変えるテストがあるので、テストごとに新しい試合を作る
    private static Match VerdyHomeMatch() => Make.ScheduledMatch(_verdy, _opponent).WithId(MatchIdValue);

    private static FakeTimeProvider ClockAt(Match match, int secondsFromKickoff) =>
        new(match.Kickoff.Utc.AddSeconds(secondsFromKickoff));

    private static Result<MyPrediction> CreateWin(Match match, TimeProvider clock) =>
        MyPrediction.Create(match, _verdy, TeamOutcome.Win, Make.TeamScoreOf(2, 1), null, clock);

    private static void ShouldBeUnchangedWin(MyPrediction prediction, DateTimeOffset updatedAt)
    {
        prediction.Choice.ShouldBe(TeamOutcome.Win);
        prediction.PredictedScore.ShouldBe(Make.TeamScoreOf(2, 1));
        prediction.Percentages.ShouldBeNull();
        prediction.EvaluationPercentages.ShouldBe(Make.PercentagesOf(60, 25, 15));
        prediction.EvaluationSource.ShouldBe(EvaluationPercentagesSource.ConvertedByRuleV1);
        prediction.UpdatedAt.ShouldBe(updatedAt);
    }
}
