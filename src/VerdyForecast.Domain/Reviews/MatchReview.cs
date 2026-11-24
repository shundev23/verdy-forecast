using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Players;
using VerdyForecast.Domain.Tags;

namespace VerdyForecast.Domain.Reviews;

/// <summary>
/// 試合後のレビュー（出場の記録、採点、MVP、戦術タグ、メモ）。1試合に1つ。
/// 参考点の計算と MVP の自動の決定は、作業指示書 2-4 で足す。
/// </summary>
public sealed class MatchReview
{
    private readonly List<PlayerAppearance> _appearances = [];
    private readonly List<PlayerRating> _ratings = [];
    private readonly List<ReviewTag> _tags = [];

    private MatchReview(MatchId matchId) => MatchId = matchId;

    public MatchId MatchId { get; private set; }

    public IReadOnlyList<PlayerAppearance> Appearances => _appearances;

    public IReadOnlyList<PlayerRating> Ratings => _ratings;

    public IReadOnlyList<ReviewTag> Tags => _tags;

    public MvpSelection? Mvp { get; private set; }

    /// <summary>メモ。最大文字数は作業指示書の D10（3-3）で決めて、入力の検証で確かめる。</summary>
    public string Memo { get; private set; } = string.Empty;

    public static MatchReview Start(MatchId matchId) => new(matchId);

    /// <summary>出場の記録を入れる。同じ選手の記録があれば置き換える。</summary>
    public void RecordAppearance(PlayerAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        var index = _appearances.FindIndex(a => a.PlayerId == appearance.PlayerId);
        if (index >= 0)
        {
            _appearances[index] = appearance;
        }
        else
        {
            _appearances.Add(appearance);
        }
    }

    /// <summary>出場の記録を消す。その選手の採点も消して、MVP だったときは MVP もなしにする。</summary>
    public void RemoveAppearance(PlayerId playerId)
    {
        _appearances.RemoveAll(a => a.PlayerId == playerId);
        _ratings.RemoveAll(r => r.PlayerId == playerId);
        if (Mvp?.PlayerId == playerId)
        {
            Mvp = null;
        }
    }

    /// <summary>最終点を付ける。出場した選手だけ（出場15分未満の選手にも付けられる。作業指示書 D6）。</summary>
    public Result RateFinal(PlayerId playerId, RatingScore score)
    {
        ArgumentNullException.ThrowIfNull(score);
        if (!HasAppeared(playerId))
        {
            return Result.Failure(ReviewErrors.PlayerNotAppeared);
        }

        var index = _ratings.FindIndex(r => r.PlayerId == playerId);
        if (index >= 0)
        {
            _ratings[index] = _ratings[index] with { Final = score };
        }
        else
        {
            _ratings.Add(new PlayerRating(playerId, Reference: null, Final: score));
        }

        return Result.Success();
    }

    /// <summary>MVP を手で選ぶ。出場した選手だけ。</summary>
    public Result ChooseMvp(PlayerId playerId)
    {
        if (!HasAppeared(playerId))
        {
            return Result.Failure(ReviewErrors.PlayerNotAppeared);
        }

        Mvp = new MvpSelection(playerId, IsManual: true);
        return Result.Success();
    }

    /// <summary>戦術タグを付ける。同じタグがすでにあれば、何もしない。</summary>
    public void AddTag(TagId tagId, TagSource source)
    {
        Guard.Defined(source);
        if (_tags.Exists(t => t.TagId == tagId))
        {
            return;
        }

        _tags.Add(new ReviewTag(tagId, source));
    }

    public void RemoveTag(TagId tagId) => _tags.RemoveAll(t => t.TagId == tagId);

    public void UpdateMemo(string? memo) => Memo = memo ?? string.Empty;

    private bool HasAppeared(PlayerId playerId) => _appearances.Exists(a => a.PlayerId == playerId);
}
