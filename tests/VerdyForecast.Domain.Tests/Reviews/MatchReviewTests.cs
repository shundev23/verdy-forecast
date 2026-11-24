using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Players;
using VerdyForecast.Domain.Reviews;
using VerdyForecast.Domain.Tags;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Reviews;

public class MatchReviewTests
{
    private static readonly MatchId _matchId = new(10);
    private static readonly PlayerId _player1 = new(1);
    private static readonly PlayerId _player2 = new(2);
    private static readonly TagId _tag1 = new(1);
    private static readonly TagId _tag2 = new(2);

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
