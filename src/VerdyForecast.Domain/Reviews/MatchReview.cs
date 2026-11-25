using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Players;
using VerdyForecast.Domain.Tags;
using VerdyForecast.Domain.Teams;

namespace VerdyForecast.Domain.Reviews;

/// <summary>
/// 試合後のレビュー（出場の記録、採点、MVP、戦術タグ、メモ）。1試合に1つ。
/// 参考点は <see cref="RecalculateReferenceScores"/>、MVP の自動の決定は <see cref="UpdateAutomaticMvp"/> で行う（作業指示書 2-4）。
/// 出場の記録や採点を直したあとに、これらを呼び直すのは、呼ぶ側（Application）の役目。
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

    /// <summary>
    /// 出場した全員の参考点を、作業指示書 D6 のルール v1 で計算し直す。自分が付けた最終点はそのまま残す。
    /// 終わった試合だけ。出場した選手が1人でも名簿にいなければ、何も変えずに失敗を返す。
    /// ポジションは名簿の今の値を使う（計算した参考点は保存するので、あとでポジションを変えても過去の参考点は変わらない）。
    /// </summary>
    public Result RecalculateReferenceScores(Match match, TeamId verdyTeamId, IReadOnlyCollection<Player> roster)
    {
        EnsureSameMatch(match);
        ArgumentNullException.ThrowIfNull(roster);

        if (match.Score is null)
        {
            return Result.Failure(ReviewErrors.MatchNotFinished);
        }

        var verdySide = match.SideOf(verdyTeamId);
        if (verdySide is null)
        {
            return Result.Failure(ReviewErrors.NotVerdyMatch);
        }

        var players = roster.ToDictionary(p => p.Id);
        if (!_appearances.TrueForAll(a => players.ContainsKey(a.PlayerId)))
        {
            return Result.Failure(ReviewErrors.PlayerNotInRoster);
        }

        var verdyScore = match.Score.ToTeamScore(verdySide.Value);
        foreach (var appearance in _appearances)
        {
            var reference = ReferenceScoreRule.CalculateV1(appearance, players[appearance.PlayerId].Position, verdyScore);
            SetReference(appearance.PlayerId, reference);
        }

        return Result.Success();
    }

    /// <summary>
    /// MVP を自動で決め直す（作業指示書 D7）。手で選んだ MVP があるときは、何もしない。
    /// 候補は最終点のある選手だけで、最終点 → 得点 → アシスト → 出場時間 → 背番号の小さい順で比べる。
    /// 候補がいなければ MVP はなし。
    /// </summary>
    public Result UpdateAutomaticMvp(IReadOnlyCollection<Player> roster)
    {
        ArgumentNullException.ThrowIfNull(roster);
        return Mvp is { IsManual: true } ? Result.Success() : DecideAutomaticMvp(roster);
    }

    /// <summary>手で選んだ MVP をやめて、自動で決め直す。失敗したときは、手で選んだ MVP のまま。</summary>
    public Result ResetMvpToAutomatic(IReadOnlyCollection<Player> roster)
    {
        ArgumentNullException.ThrowIfNull(roster);
        return DecideAutomaticMvp(roster);
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

    // 参考点を入れる。最終点はそのまま。参考点も最終点もなくなった選手は、採点の一覧から外す
    private void SetReference(PlayerId playerId, RatingScore? reference)
    {
        var index = _ratings.FindIndex(r => r.PlayerId == playerId);
        if (index < 0)
        {
            if (reference is not null)
            {
                _ratings.Add(new PlayerRating(playerId, Reference: reference, Final: null));
            }

            return;
        }

        var updated = _ratings[index] with { Reference = reference };
        if (updated.Reference is null && updated.Final is null)
        {
            _ratings.RemoveAt(index);
        }
        else
        {
            _ratings[index] = updated;
        }
    }

    private Result DecideAutomaticMvp(IReadOnlyCollection<Player> roster)
    {
        var shirtNumbers = roster.ToDictionary(p => p.Id, p => p.ShirtNumber.Value);
        var candidates = _ratings
            .Where(r => r.Final is not null)
            .Select(r => (Rating: r, Appearance: _appearances.Single(a => a.PlayerId == r.PlayerId)))
            .ToList();

        if (!candidates.TrueForAll(c => shirtNumbers.ContainsKey(c.Rating.PlayerId)))
        {
            return Result.Failure(ReviewErrors.PlayerNotInRoster);
        }

        // 背番号まで同じ（名簿の誤り）でも結果が毎回同じになるよう、最後に ID で比べる
        var best = candidates
            .OrderByDescending(c => c.Rating.Final!.Value)
            .ThenByDescending(c => c.Appearance.Goals)
            .ThenByDescending(c => c.Appearance.Assists)
            .ThenByDescending(c => c.Appearance.Minutes)
            .ThenBy(c => shirtNumbers[c.Rating.PlayerId])
            .ThenBy(c => c.Rating.PlayerId.Value)
            .Select(c => (PlayerId?)c.Rating.PlayerId)
            .FirstOrDefault();

        Mvp = best is null ? null : new MvpSelection(best.Value, IsManual: false);
        return Result.Success();
    }

    private void EnsureSameMatch(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        if (match.Id != MatchId)
        {
            throw new ArgumentException("このレビューとは別の試合です。", nameof(match));
        }
    }
}
