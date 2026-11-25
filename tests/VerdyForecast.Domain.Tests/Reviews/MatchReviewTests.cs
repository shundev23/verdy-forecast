using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Players;
using VerdyForecast.Domain.Reviews;
using VerdyForecast.Domain.Tags;
using VerdyForecast.Domain.Teams;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Reviews;

public class MatchReviewTests
{
    private static readonly MatchId _matchId = new(10);
    private static readonly PlayerId _player1 = new(1);
    private static readonly PlayerId _player2 = new(2);
    private static readonly TagId _tag1 = new(1);
    private static readonly TagId _tag2 = new(2);
    private static readonly TeamId _verdy = new(1);
    private static readonly TeamId _opponent = new(2);
    private static readonly TeamId _otherTeam = new(3);

    // 名簿: 選手1は DF（背番号1）、選手2は FW（背番号2）
    private static readonly Player[] _roster = [Make.PlayerOf(1, Position.Defender), Make.PlayerOf(2, Position.Forward)];

    [Fact]
    public void Start_IsEmpty()
    {
        var review = MatchReview.Start(_matchId);

        review.MatchId.ShouldBe(_matchId);
        review.Appearances.ShouldBeEmpty();
        review.Ratings.ShouldBeEmpty();
        review.Tags.ShouldBeEmpty();
        review.Mvp.ShouldBeNull();
        review.Memo.ShouldBe(string.Empty);
    }

    [Fact]
    public void RecordAppearance_SamePlayerTwice_ReplacesRecord()
    {
        var review = MatchReview.Start(_matchId);

        review.RecordAppearance(Make.AppearanceOf(_player1, minutes: 45));
        review.RecordAppearance(Make.AppearanceOf(_player1, minutes: 90, goals: 1));

        review.Appearances.ShouldHaveSingleItem().ShouldBe(Make.AppearanceOf(_player1, minutes: 90, goals: 1));
    }

    [Fact]
    public void RateFinal_AppearedPlayer_AddsFinalWithoutReference()
    {
        var review = ReviewWithAppearances(_player1);

        review.RateFinal(_player1, Make.RatingOf(6.5m)).ShouldSucceed();

        review.Ratings.ShouldHaveSingleItem().ShouldBe(new PlayerRating(_player1, Reference: null, Final: Make.RatingOf(6.5m)));
    }

    [Fact]
    public void RateFinal_SamePlayerTwice_UpdatesFinal()
    {
        var review = ReviewWithAppearances(_player1);
        review.RateFinal(_player1, Make.RatingOf(6.5m)).ShouldSucceed();

        review.RateFinal(_player1, Make.RatingOf(7.0m)).ShouldSucceed();

        review.Ratings.ShouldHaveSingleItem().Final.ShouldBe(Make.RatingOf(7.0m));
    }

    [Fact]
    public void RateFinal_PlayerNotAppeared_Fails()
    {
        var review = ReviewWithAppearances(_player1);

        review.RateFinal(_player2, Make.RatingOf(6.0m)).ShouldFailWith(ReviewErrors.PlayerNotAppeared);

        review.Ratings.ShouldBeEmpty();
    }

    [Fact]
    public void ChooseMvp_AppearedPlayer_SetsManualMvp()
    {
        var review = ReviewWithAppearances(_player1, _player2);

        review.ChooseMvp(_player2).ShouldSucceed();

        review.Mvp.ShouldBe(new MvpSelection(_player2, IsManual: true));
    }

    [Fact]
    public void ChooseMvp_PlayerNotAppeared_FailsAndKeepsMvp()
    {
        var review = ReviewWithAppearances(_player1);
        review.ChooseMvp(_player1).ShouldSucceed();

        review.ChooseMvp(_player2).ShouldFailWith(ReviewErrors.PlayerNotAppeared);

        review.Mvp.ShouldBe(new MvpSelection(_player1, IsManual: true));
    }

    [Fact]
    public void RemoveAppearance_MvpPlayer_RemovesRatingAndClearsMvp()
    {
        var review = ReviewWithAppearances(_player1, _player2);
        review.RateFinal(_player1, Make.RatingOf(8.0m)).ShouldSucceed();
        review.ChooseMvp(_player1).ShouldSucceed();

        review.RemoveAppearance(_player1);

        review.Appearances.ShouldHaveSingleItem().PlayerId.ShouldBe(_player2);
        review.Ratings.ShouldBeEmpty();
        review.Mvp.ShouldBeNull();
    }

    [Fact]
    public void RemoveAppearance_OtherPlayer_KeepsMvp()
    {
        var review = ReviewWithAppearances(_player1, _player2);
        review.ChooseMvp(_player1).ShouldSucceed();

        review.RemoveAppearance(_player2);

        review.Mvp.ShouldBe(new MvpSelection(_player1, IsManual: true));
    }

    [Fact]
    public void AddTag_SameTagTwice_KeepsFirst()
    {
        var review = MatchReview.Start(_matchId);

        review.AddTag(_tag1, TagSource.Suggested);
        review.AddTag(_tag1, TagSource.Manual);

        review.Tags.ShouldHaveSingleItem().ShouldBe(new ReviewTag(_tag1, TagSource.Suggested));
    }

    [Fact]
    public void RemoveTag_RemovesOnlyThatTag()
    {
        var review = MatchReview.Start(_matchId);
        review.AddTag(_tag1, TagSource.Manual);
        review.AddTag(_tag2, TagSource.Manual);

        review.RemoveTag(_tag1);

        review.Tags.ShouldHaveSingleItem().TagId.ShouldBe(_tag2);
    }

    [Fact]
    public void AddTag_UndefinedSource_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => MatchReview.Start(_matchId).AddTag(_tag1, (TagSource)99));

    [Fact]
    public void UpdateMemo_Text_StoresAsIs()
    {
        var review = MatchReview.Start(_matchId);

        review.UpdateMemo("前半の守備が良かった。<b>太字</b> はそのまま文字として残す。");

        review.Memo.ShouldBe("前半の守備が良かった。<b>太字</b> はそのまま文字として残す。");
    }

    [Fact]
    public void UpdateMemo_Null_StoresEmpty()
    {
        var review = MatchReview.Start(_matchId);
        review.UpdateMemo("メモ");

        review.UpdateMemo(null);

        review.Memo.ShouldBe(string.Empty);
    }

    // ---- 参考点（作業指示書 D6） ----

    [Fact]
    public void RecalculateReferenceScores_FinishedMatch_SetsReferenceAndKeepsFinal()
    {
        var review = MatchReview.Start(_matchId);
        review.RecordAppearance(Make.AppearanceOf(_player1, minutes: 90));
        review.RecordAppearance(Make.AppearanceOf(_player2, minutes: 10));
        review.RateFinal(_player1, Make.RatingOf(6.5m)).ShouldSucceed();
        review.RateFinal(_player2, Make.RatingOf(5.5m)).ShouldSucceed();

        review.RecalculateReferenceScores(FinishedMatch(_verdy, _opponent, 2, 0), _verdy, _roster).ShouldSucceed();

        // 選手1（DF、90分、2-0 の勝ち、無失点）: 6.0 + 0.5 + 0.5 = 7.0。選手2 は15分未満なので参考点なし
        review.Ratings.ShouldBe(
        [
            new PlayerRating(_player1, Reference: Make.RatingOf(7.0m), Final: Make.RatingOf(6.5m)),
            new PlayerRating(_player2, Reference: null, Final: Make.RatingOf(5.5m)),
        ]);
    }

    [Fact]
    public void RecalculateReferenceScores_VerdyAway_UsesVerdyPerspective()
    {
        var review = ReviewWithAppearances(_player1);

        // ヴェルディがアウェイで 2-0 の勝ち（ホームから見ると 0-2）
        review.RecalculateReferenceScores(FinishedMatch(_opponent, _verdy, 0, 2), _verdy, _roster).ShouldSucceed();

        review.Ratings.ShouldHaveSingleItem().ShouldBe(new PlayerRating(_player1, Reference: Make.RatingOf(7.0m), Final: null));
    }

    [Fact]
    public void RecalculateReferenceScores_UnderFifteenMinutesWithoutFinal_HasNoRating()
    {
        var review = MatchReview.Start(_matchId);
        review.RecordAppearance(Make.AppearanceOf(_player2, minutes: 14));

        review.RecalculateReferenceScores(FinishedMatch(_verdy, _opponent, 1, 1), _verdy, _roster).ShouldSucceed();

        review.Ratings.ShouldBeEmpty();
    }

    [Fact]
    public void RecalculateReferenceScores_AfterMinutesReduced_RemovesReference()
    {
        var review = ReviewWithAppearances(_player1);
        var match = FinishedMatch(_verdy, _opponent, 1, 1);
        review.RecalculateReferenceScores(match, _verdy, _roster).ShouldSucceed();
        review.RecordAppearance(Make.AppearanceOf(_player1, minutes: 10));

        review.RecalculateReferenceScores(match, _verdy, _roster).ShouldSucceed();

        review.Ratings.ShouldBeEmpty();
    }

    [Fact]
    public void RecalculateReferenceScores_MatchNotFinished_Fails()
    {
        var review = ReviewWithAppearances(_player1);
        var match = Make.ScheduledMatch(_verdy, _opponent).WithId(_matchId.Value);

        review.RecalculateReferenceScores(match, _verdy, _roster).ShouldFailWith(ReviewErrors.MatchNotFinished);
    }

    [Fact]
    public void RecalculateReferenceScores_MatchWithoutVerdy_Fails()
    {
        var review = ReviewWithAppearances(_player1);

        review.RecalculateReferenceScores(FinishedMatch(_opponent, _otherTeam, 1, 0), _verdy, _roster)
            .ShouldFailWith(ReviewErrors.NotVerdyMatch);
    }

    [Fact]
    public void RecalculateReferenceScores_PlayerNotInRoster_FailsWithoutChanges()
    {
        var review = ReviewWithAppearances(_player1, _player2);

        review.RecalculateReferenceScores(FinishedMatch(_verdy, _opponent, 1, 0), _verdy, [_roster[0]])
            .ShouldFailWith(ReviewErrors.PlayerNotInRoster);

        review.Ratings.ShouldBeEmpty();
    }

    [Fact]
    public void RecalculateReferenceScores_DifferentMatch_Throws()
    {
        var review = ReviewWithAppearances(_player1);
        var otherMatch = FinishedMatch(_verdy, _opponent, 1, 0).WithId(_matchId.Value + 1);

        Should.Throw<ArgumentException>(() => review.RecalculateReferenceScores(otherMatch, _verdy, _roster));
    }

    // ---- MVP の自動の決定（作業指示書 D7） ----

    [Fact]
    public void UpdateAutomaticMvp_HighestFinal_BecomesAutomaticMvp()
    {
        var review = ReviewWithFinals((_player1, 6.5m), (_player2, 7.0m));

        review.UpdateAutomaticMvp(_roster).ShouldSucceed();

        review.Mvp.ShouldBe(new MvpSelection(_player2, IsManual: false));
    }

    [Fact]
    public void UpdateAutomaticMvp_FinalTied_MoreGoalsWins()
    {
        var review = MatchReview.Start(_matchId);
        review.RecordAppearance(Make.AppearanceOf(_player2, goals: 0, assists: 3, minutes: 90));
        review.RecordAppearance(Make.AppearanceOf(_player1, goals: 1, assists: 0, minutes: 30));
        RateBoth(review, 7.0m);

        review.UpdateAutomaticMvp(_roster).ShouldSucceed();

        review.Mvp!.PlayerId.ShouldBe(_player1);
    }

    [Fact]
    public void UpdateAutomaticMvp_GoalsTied_MoreAssistsWins()
    {
        var review = MatchReview.Start(_matchId);
        review.RecordAppearance(Make.AppearanceOf(_player2, goals: 1, assists: 0, minutes: 90));
        review.RecordAppearance(Make.AppearanceOf(_player1, goals: 1, assists: 1, minutes: 30));
        RateBoth(review, 7.0m);

        review.UpdateAutomaticMvp(_roster).ShouldSucceed();

        review.Mvp!.PlayerId.ShouldBe(_player1);
    }

    [Fact]
    public void UpdateAutomaticMvp_AssistsTied_LongerMinutesWins()
    {
        var review = MatchReview.Start(_matchId);
        review.RecordAppearance(Make.AppearanceOf(_player1, goals: 1, assists: 1, minutes: 60));
        review.RecordAppearance(Make.AppearanceOf(_player2, goals: 1, assists: 1, minutes: 90));
        RateBoth(review, 7.0m);

        review.UpdateAutomaticMvp(_roster).ShouldSucceed();

        review.Mvp!.PlayerId.ShouldBe(_player2);
    }

    [Fact]
    public void UpdateAutomaticMvp_MinutesTied_SmallerShirtNumberWins()
    {
        var review = MatchReview.Start(_matchId);
        review.RecordAppearance(Make.AppearanceOf(_player1, minutes: 90));
        review.RecordAppearance(Make.AppearanceOf(_player2, minutes: 90));
        RateBoth(review, 7.0m);
        Player[] roster = [Make.PlayerOf(1, Position.Defender, shirtNumber: 20), Make.PlayerOf(2, Position.Forward, shirtNumber: 8)];

        review.UpdateAutomaticMvp(roster).ShouldSucceed();

        review.Mvp!.PlayerId.ShouldBe(_player2);
    }

    [Fact]
    public void UpdateAutomaticMvp_OnlyPlayersWithFinalAreCandidates()
    {
        var review = MatchReview.Start(_matchId);
        review.RecordAppearance(Make.AppearanceOf(_player1, goals: 3));
        review.RecordAppearance(Make.AppearanceOf(_player2));
        review.RecalculateReferenceScores(FinishedMatch(_verdy, _opponent, 3, 0), _verdy, _roster).ShouldSucceed();
        review.RateFinal(_player2, Make.RatingOf(5.0m)).ShouldSucceed();

        review.UpdateAutomaticMvp(_roster).ShouldSucceed();

        review.Mvp!.PlayerId.ShouldBe(_player2);
    }

    [Fact]
    public void UpdateAutomaticMvp_NoFinals_HasNoMvp()
    {
        var review = ReviewWithAppearances(_player1, _player2);

        review.UpdateAutomaticMvp(_roster).ShouldSucceed();

        review.Mvp.ShouldBeNull();
    }

    [Fact]
    public void UpdateAutomaticMvp_AfterFinalChanged_FollowsNewHighest()
    {
        var review = ReviewWithFinals((_player1, 7.0m), (_player2, 6.0m));
        review.UpdateAutomaticMvp(_roster).ShouldSucceed();
        review.RateFinal(_player2, Make.RatingOf(8.0m)).ShouldSucceed();

        review.UpdateAutomaticMvp(_roster).ShouldSucceed();

        review.Mvp.ShouldBe(new MvpSelection(_player2, IsManual: false));
    }

    [Fact]
    public void UpdateAutomaticMvp_ManualMvp_IsKept()
    {
        var review = ReviewWithFinals((_player1, 6.0m), (_player2, 8.0m));
        review.ChooseMvp(_player1).ShouldSucceed();

        review.UpdateAutomaticMvp(_roster).ShouldSucceed();

        review.Mvp.ShouldBe(new MvpSelection(_player1, IsManual: true));
    }

    [Fact]
    public void UpdateAutomaticMvp_CandidateNotInRoster_Fails()
    {
        var review = ReviewWithFinals((_player1, 6.0m), (_player2, 8.0m));

        review.UpdateAutomaticMvp([_roster[0]]).ShouldFailWith(ReviewErrors.PlayerNotInRoster);

        review.Mvp.ShouldBeNull();
    }

    [Fact]
    public void ResetMvpToAutomatic_ReplacesManualMvp()
    {
        var review = ReviewWithFinals((_player1, 6.0m), (_player2, 8.0m));
        review.ChooseMvp(_player1).ShouldSucceed();

        review.ResetMvpToAutomatic(_roster).ShouldSucceed();

        review.Mvp.ShouldBe(new MvpSelection(_player2, IsManual: false));
    }

    [Fact]
    public void ResetMvpToAutomatic_CandidateNotInRoster_FailsAndKeepsManualMvp()
    {
        var review = ReviewWithFinals((_player1, 6.0m), (_player2, 8.0m));
        review.ChooseMvp(_player1).ShouldSucceed();

        review.ResetMvpToAutomatic([_roster[0]]).ShouldFailWith(ReviewErrors.PlayerNotInRoster);

        review.Mvp.ShouldBe(new MvpSelection(_player1, IsManual: true));
    }

    private static Match FinishedMatch(TeamId homeTeamId, TeamId awayTeamId, int homeGoals, int awayGoals)
    {
        var match = Make.ScheduledMatch(homeTeamId, awayTeamId).WithId(_matchId.Value);
        match.RecordResult(Make.ScoreOf(homeGoals, awayGoals)).ShouldSucceed();
        return match;
    }

    private static MatchReview ReviewWithFinals(params (PlayerId PlayerId, decimal Final)[] finals)
    {
        var review = MatchReview.Start(_matchId);
        foreach (var (playerId, final) in finals)
        {
            review.RecordAppearance(Make.AppearanceOf(playerId));
            review.RateFinal(playerId, Make.RatingOf(final)).ShouldSucceed();
        }

        return review;
    }

    private static void RateBoth(MatchReview review, decimal final)
    {
        review.RateFinal(_player1, Make.RatingOf(final)).ShouldSucceed();
        review.RateFinal(_player2, Make.RatingOf(final)).ShouldSucceed();
    }

    private static MatchReview ReviewWithAppearances(params PlayerId[] playerIds)
    {
        var review = MatchReview.Start(_matchId);
        foreach (var playerId in playerIds)
        {
            review.RecordAppearance(Make.AppearanceOf(playerId));
        }

        return review;
    }
}
