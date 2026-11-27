using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Ratings;
using VerdyForecast.Domain.Teams;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Ratings;

public class RatingTableTests
{
    private const double Tolerance = 1e-4;
    private static readonly TeamId _team1 = new(1);
    private static readonly TeamId _team2 = new(2);
    private static readonly TeamId _team3 = new(3);
    private static readonly TeamId _team4 = new(4);
    private static readonly TeamId _team5 = new(5);

    // ---- シーズンを始める ----

    [Fact]
    public void StartSeason_First_AllTeamsStartAt1500()
    {
        var table = RatingTable.Create(EloSettings.Default);

        table.StartSeason(Make.SeasonOf("2023"), [_team1, _team2, _team3]).ShouldSucceed();

        table.CurrentSeason.ShouldBe(Make.SeasonOf("2023"));
        table.Ratings.Count.ShouldBe(3);
        table.Ratings.Values.ShouldAllBe(rating => rating == Elo.InitialRating);
    }

    [Fact]
    public void StartSeason_Second_CarriesOverTowardMeanAndPromotesWithBottomThreeAverage()
    {
        // 前のシーズン（平均 1500）。c = 0.8 で引き戻すと 1580、1500、1460、1460
        var table = Restore(new() { [_team1] = 1600, [_team2] = 1500, [_team3] = 1450, [_team4] = 1450 });

        table.StartSeason(Make.SeasonOf("2026"), [_team1, _team2, _team3, _team5]).ShouldSucceed();

        table.Ratings[_team1].ShouldBe(1580, Tolerance);
        table.Ratings[_team2].ShouldBe(1500, Tolerance);
        table.Ratings[_team3].ShouldBe(1460, Tolerance);
        // 昇格チームは、引き戻したあとの下位3チーム（1460、1460、1500）の平均
        table.Ratings[_team5].ShouldBe((1460 + 1460 + 1500) / 3.0, Tolerance);
        // 降格したチームは表から外れる
        table.Ratings.ContainsKey(_team4).ShouldBeFalse();
    }

    [Fact]
    public void StartSeason_Second_UsesMeanOfPreviousSeason()
    {
        // 平均は 1525。引き戻すと 1585、1585、1425、1505
        var table = Restore(new() { [_team1] = 1600, [_team2] = 1600, [_team3] = 1400, [_team4] = 1500 });

        table.StartSeason(Make.SeasonOf("2026"), [_team1, _team3, _team4, _team5]).ShouldSucceed();

        table.Ratings[_team1].ShouldBe(1585, Tolerance);
        table.Ratings[_team3].ShouldBe(1425, Tolerance);
        table.Ratings[_team4].ShouldBe(1505, Tolerance);
        table.Ratings[_team5].ShouldBe((1425 + 1505 + 1585) / 3.0, Tolerance);
    }

    [Fact]
    public void StartSeason_CarryOverOne_KeepsRatings()
    {
        var settings = EloSettings.Create(k: 20, homeAdvantage: 50, carryOver: 1).ShouldSucceed();
        var table = RatingTable.Restore(settings, Make.SeasonOf("2025"), new Dictionary<TeamId, double> { [_team1] = 1600, [_team2] = 1400 }, null).ShouldSucceed();

        table.StartSeason(Make.SeasonOf("2026"), [_team1, _team2]).ShouldSucceed();

        table.Ratings[_team1].ShouldBe(1600, Tolerance);
        table.Ratings[_team2].ShouldBe(1400, Tolerance);
    }

    [Fact]
    public void StartSeason_SameRatingsInDifferentOrder_GiveExactlySameValues()
    {
        // 足し算の順番で結果が変わる値（1e16 + 1 は 1e16 に丸まる）。
        // 順番が 1, 2, 3 なら合計は 0、1, 3, 2 なら 1 になるので、順番を固定していないと平均がずれる
        var inOrder = Restore(new() { [_team1] = 1e16, [_team2] = 1, [_team3] = -1e16 });
        var reordered = Restore(new() { [_team1] = 1e16, [_team3] = -1e16, [_team2] = 1 });

        inOrder.StartSeason(Make.SeasonOf("2026"), [_team1, _team2, _team3]).ShouldSucceed();
        reordered.StartSeason(Make.SeasonOf("2026"), [_team1, _team2, _team3]).ShouldSucceed();

        reordered.Ratings[_team1].ShouldBe(inOrder.Ratings[_team1]);
        reordered.Ratings[_team2].ShouldBe(inOrder.Ratings[_team2]);
        reordered.Ratings[_team3].ShouldBe(inOrder.Ratings[_team3]);
    }

    [Fact]
    public void StartSeason_OneTeam_Fails() =>
        RatingTable.Create(EloSettings.Default).StartSeason(Make.SeasonOf("2023"), [_team1]).ShouldFailWith(RatingErrors.TooFewTeams);

    [Fact]
    public void StartSeason_DuplicateTeams_Fails() =>
        RatingTable.Create(EloSettings.Default).StartSeason(Make.SeasonOf("2023"), [_team1, _team2, _team1]).ShouldFailWith(RatingErrors.DuplicateTeams);

    [Fact]
    public void StartSeason_SameSeasonTwice_Fails()
    {
        var table = RatingTable.Create(EloSettings.Default);
        table.StartSeason(Make.SeasonOf("2023"), [_team1, _team2]).ShouldSucceed();

        table.StartSeason(Make.SeasonOf("2023"), [_team1, _team2]).ShouldFailWith(RatingErrors.SeasonAlreadyStarted);
    }

    [Fact]
    public void Restore_OneTeam_Fails() =>
        RatingTable.Restore(EloSettings.Default, Make.SeasonOf("2025"), new Dictionary<TeamId, double> { [_team1] = 1500 }, null)
            .ShouldFailWith(RatingErrors.TooFewTeams);

    // ---- 試合を反映する ----

    [Fact]
    public void Apply_HomeWinsByOne_UpdatesBothTeamsAndReturnsChange()
    {
        var table = FirstSeasonTable();
        var match = FinishedMatch(day: 1, _team1, _team2, 1, 0);

        var change = table.Apply(match).ShouldSucceed();

        change.MatchId.ShouldBe(match.Id);
        change.Kickoff.ShouldBe(match.Kickoff);
        change.Home.TeamId.ShouldBe(_team1);
        change.Home.Before.ShouldBe(1500);
        change.Home.After.ShouldBe(1508.5707, Tolerance);
        change.Away.TeamId.ShouldBe(_team2);
        change.Away.Before.ShouldBe(1500);
        change.Away.After.ShouldBe(1491.4293, Tolerance);
        table.Ratings[_team1].ShouldBe(change.Home.After);
        table.Ratings[_team2].ShouldBe(change.Away.After);
        table.LastKickoff.ShouldBe(match.Kickoff);
    }

    [Fact]
    public void Apply_SeveralMatches_KeepsTotalOfRatings()
    {
        var table = FirstSeasonTable();

        table.Apply(FinishedMatch(day: 1, _team1, _team2, 3, 0)).ShouldSucceed();
        table.Apply(FinishedMatch(day: 2, _team3, _team1, 2, 2)).ShouldSucceed();
        table.Apply(FinishedMatch(day: 3, _team2, _team3, 0, 1)).ShouldSucceed();

        table.Ratings.Values.Sum().ShouldBe(3 * Elo.InitialRating, Tolerance);
    }

    [Fact]
    public void Apply_SameKickoffAsLast_Succeeds()
    {
        var table = RatingTable.Create(EloSettings.Default);
        table.StartSeason(Make.SeasonOf(), [_team1, _team2, _team3, _team4]).ShouldSucceed();
        table.Apply(FinishedMatch(day: 1, _team1, _team2, 1, 0)).ShouldSucceed();

        table.Apply(FinishedMatch(day: 1, _team3, _team4, 0, 0)).ShouldSucceed();
    }

    [Fact]
    public void Apply_EarlierKickoffThanLast_FailsAndKeepsRatings()
    {
        var table = FirstSeasonTable();
        table.Apply(FinishedMatch(day: 2, _team1, _team2, 1, 0)).ShouldSucceed();
        var ratingsBefore = table.Ratings.ToDictionary();

        table.Apply(FinishedMatch(day: 1, _team2, _team3, 1, 0)).ShouldFailWith(RatingErrors.OutOfOrder);

        table.Ratings.ShouldBe(ratingsBefore);
    }

    [Fact]
    public void Apply_EarlierThanRestoredLastKickoff_FailsEvenInNewSeason()
    {
        var table = RatingTable.Restore(
            EloSettings.Default,
            Make.SeasonOf("2025"),
            new Dictionary<TeamId, double> { [_team1] = 1500, [_team2] = 1500 },
            lastKickoff: Make.KickoffAt(day: 20)).ShouldSucceed();
        table.StartSeason(Make.SeasonOf(), [_team1, _team2]).ShouldSucceed();

        table.Apply(FinishedMatch(day: 10, _team1, _team2, 1, 0)).ShouldFailWith(RatingErrors.OutOfOrder);
    }

    [Fact]
    public void Apply_BeforeAnySeason_Fails() =>
        RatingTable.Create(EloSettings.Default).Apply(FinishedMatch(day: 1, _team1, _team2, 1, 0)).ShouldFailWith(RatingErrors.NoSeasonStarted);

    [Fact]
    public void Apply_MatchOfOtherSeason_Fails()
    {
        var table = RatingTable.Create(EloSettings.Default);
        table.StartSeason(Make.SeasonOf("2025"), [_team1, _team2]).ShouldSucceed();

        table.Apply(FinishedMatch(day: 1, _team1, _team2, 1, 0)).ShouldFailWith(RatingErrors.SeasonMismatch);
    }

    [Fact]
    public void Apply_MatchNotFinished_Fails() =>
        FirstSeasonTable().Apply(Make.ScheduledMatch(_team1, _team2)).ShouldFailWith(RatingErrors.MatchNotFinished);

    [Fact]
    public void Apply_TeamNotInSeason_Fails() =>
        FirstSeasonTable().Apply(FinishedMatch(day: 1, _team1, _team5, 1, 0)).ShouldFailWith(RatingErrors.TeamNotInSeason);

    // ---- テスト用の補助 ----

    // 2026-27 シーズンを、チーム1〜3で始めた表
    private static RatingTable FirstSeasonTable()
    {
        var table = RatingTable.Create(EloSettings.Default);
        table.StartSeason(Make.SeasonOf(), [_team1, _team2, _team3]).ShouldSucceed();
        return table;
    }

    private static RatingTable Restore(Dictionary<TeamId, double> ratings) =>
        RatingTable.Restore(EloSettings.Default, Make.SeasonOf("2025"), ratings, lastKickoff: null).ShouldSucceed();

    // 2026-27 シーズンの、2026年11月の指定した日の試合（終了済み）
    private static Match FinishedMatch(int day, TeamId home, TeamId away, int homeGoals, int awayGoals)
    {
        var match = Match.Schedule(Make.SeasonOf(), Make.KickoffAt(day: day), home, away).ShouldSucceed();
        match.RecordResult(Make.ScoreOf(homeGoals, awayGoals)).ShouldSucceed();
        return match;
    }
}
