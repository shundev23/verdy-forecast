using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Teams;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Matches;

public class MatchTests
{
    private static readonly TeamId _home = new(1);
    private static readonly TeamId _away = new(2);

    [Fact]
    public void Schedule_DifferentTeams_IsScheduledWithoutScore()
    {
        var match = Match.Schedule(Make.SeasonOf("2026-27"), Make.KickoffAt(28, 5), _home, _away).ShouldSucceed();

        match.Season.ShouldBe(Make.SeasonOf("2026-27"));
        match.Kickoff.ShouldBe(Make.KickoffAt(28, 5));
        match.HomeTeamId.ShouldBe(_home);
        match.AwayTeamId.ShouldBe(_away);
        match.Status.ShouldBe(MatchStatus.Scheduled);
        match.Score.ShouldBeNull();
    }

    [Fact]
    public void Schedule_SameTeams_Fails() =>
        Match.Schedule(Make.SeasonOf(), Make.KickoffAt(), _home, _home).ShouldFailWith(MatchErrors.SameTeams);

    [Fact]
    public void SideOf_ReturnsSideOrNull()
    {
        var match = Make.ScheduledMatch(_home, _away);

        match.SideOf(_home).ShouldBe(Side.Home);
        match.SideOf(_away).ShouldBe(Side.Away);
        match.SideOf(new TeamId(3)).ShouldBeNull();
    }

    [Fact]
    public void RecordResult_Scheduled_FinishesWithScore()
    {
        var match = Make.ScheduledMatch(_home, _away);

        match.RecordResult(Make.ScoreOf(2, 1)).ShouldSucceed();

        match.Status.ShouldBe(MatchStatus.Finished);
        match.Score.ShouldBe(Make.ScoreOf(2, 1));
    }

    [Fact]
    public void RecordResult_Finished_ReplacesScore()
    {
        var match = Make.ScheduledMatch(_home, _away);
        match.RecordResult(Make.ScoreOf(2, 1)).ShouldSucceed();

        match.RecordResult(Make.ScoreOf(2, 2)).ShouldSucceed();

        match.Status.ShouldBe(MatchStatus.Finished);
        match.Score.ShouldBe(Make.ScoreOf(2, 2));
    }

    [Fact]
    public void RecordResult_Postponed_FailsAndKeepsState()
    {
        var match = Make.ScheduledMatch(_home, _away);
        match.Postpone().ShouldSucceed();

        match.RecordResult(Make.ScoreOf(2, 1)).ShouldFailWith(MatchErrors.Postponed);

        match.Status.ShouldBe(MatchStatus.Postponed);
        match.Score.ShouldBeNull();
    }

    [Fact]
    public void Postpone_Scheduled_BecomesPostponed()
    {
        var match = Make.ScheduledMatch(_home, _away);

        match.Postpone().ShouldSucceed();

        match.Status.ShouldBe(MatchStatus.Postponed);
    }

    [Fact]
    public void Postpone_Finished_FailsAndKeepsState()
    {
        var match = Make.ScheduledMatch(_home, _away);
        match.RecordResult(Make.ScoreOf(0, 0)).ShouldSucceed();

        match.Postpone().ShouldFailWith(MatchErrors.AlreadyFinished);

        match.Status.ShouldBe(MatchStatus.Finished);
    }

    [Fact]
    public void Reschedule_Postponed_SetsKickoffAndBecomesScheduled()
    {
        var match = Make.ScheduledMatch(_home, _away);
        match.Postpone().ShouldSucceed();

        match.Reschedule(Make.KickoffAt(day: 30, hour: 10)).ShouldSucceed();

        match.Kickoff.ShouldBe(Make.KickoffAt(day: 30, hour: 10));
        match.Status.ShouldBe(MatchStatus.Scheduled);
    }

    [Fact]
    public void Reschedule_Finished_FailsAndKeepsKickoff()
    {
        var match = Make.ScheduledMatch(_home, _away);
        var originalKickoff = match.Kickoff;
        match.RecordResult(Make.ScoreOf(1, 0)).ShouldSucceed();

        match.Reschedule(Make.KickoffAt(day: 30, hour: 10)).ShouldFailWith(MatchErrors.AlreadyFinished);

        match.Kickoff.ShouldBe(originalKickoff);
        match.Status.ShouldBe(MatchStatus.Finished);
    }
}
