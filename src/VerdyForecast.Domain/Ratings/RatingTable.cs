using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Teams;

namespace VerdyForecast.Domain.Ratings;

/// <summary>
/// 今のシーズンの、全チームの Elo レーティング（作業指示書 D4、2-5）。
/// シーズンを始めてから、終わった試合をキックオフの順に1つずつ反映する。
/// </summary>
public sealed class RatingTable
{
    // 昇格チームの初期値は、前のシーズンの下位（Elo の値が低い）この数のチームの平均
    private const int BottomTeamsForPromoted = 3;

    private readonly EloSettings _settings;
    private Dictionary<TeamId, double> _ratings = [];

    private RatingTable(EloSettings settings) => _settings = settings;

    public Season? CurrentSeason { get; private set; }

    /// <summary>最後に反映した試合のキックオフ。これより早い試合は反映できない。</summary>
    public KickoffTime? LastKickoff { get; private set; }

    public IReadOnlyDictionary<TeamId, double> Ratings => _ratings;

    /// <summary>まだ何も反映していない表を作る。</summary>
    public static RatingTable Create(EloSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new RatingTable(settings);
    }

    /// <summary>保存しておいた途中の状態から、表を作り直す（前回の続きから反映するとき）。</summary>
    public static Result<RatingTable> Restore(
        EloSettings settings,
        Season season,
        IReadOnlyDictionary<TeamId, double> ratings,
        KickoffTime? lastKickoff)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(season);
        ArgumentNullException.ThrowIfNull(ratings);
        if (ratings.Count < 2)
        {
            return Result.Failure<RatingTable>(RatingErrors.TooFewTeams);
        }

        var table = new RatingTable(settings)
        {
            CurrentSeason = season,
            LastKickoff = lastKickoff,
            _ratings = new Dictionary<TeamId, double>(ratings),
        };
        return Result.Success(table);
    }

    /// <summary>
    /// シーズンを始める。最初のシーズンは全チーム 1500。
    /// 2つ目からは、前のシーズンから続けて出るチームを平均に向けて引き戻し（R ← 平均 + c ×（R − 平均））、
    /// 新しく入ったチーム（昇格）には、前のシーズンの下位3チームの平均を入れる（引き戻したあとの値。作業指示書 D4）。
    /// 前のシーズンにいて、今のシーズンにいないチーム（降格）は、表から外す。
    /// </summary>
    public Result StartSeason(Season season, IReadOnlyCollection<TeamId> teams)
    {
        ArgumentNullException.ThrowIfNull(season);
        ArgumentNullException.ThrowIfNull(teams);

        var error = ValidateNewSeason(season, teams);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        _ratings = CurrentSeason is null
            ? teams.ToDictionary(team => team, _ => Elo.InitialRating)
            : CarryOverTo(teams);
        CurrentSeason = season;
        return Result.Success();
    }

    /// <summary>
    /// 終わった試合を1つ反映する。キックオフの順に渡す（前に反映した試合より早い試合は失敗）。
    /// 予測に使ってよいのは、その試合を反映する前の値だけ（データリーケージの防止）。
    /// </summary>
    public Result<RatingChange> Apply(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);

        var error = ValidateMatch(match);
        if (error is not null)
        {
            return Result.Failure<RatingChange>(error);
        }

        var homeBefore = _ratings[match.HomeTeamId];
        var awayBefore = _ratings[match.AwayTeamId];
        var change = Elo.HomeRatingChange(homeBefore, awayBefore, match.Score!, _settings);

        _ratings[match.HomeTeamId] = homeBefore + change;
        _ratings[match.AwayTeamId] = awayBefore - change;
        LastKickoff = match.Kickoff;

        return Result.Success(new RatingChange(
            match.Id,
            match.Kickoff,
            new TeamRatingChange(match.HomeTeamId, homeBefore, homeBefore + change),
            new TeamRatingChange(match.AwayTeamId, awayBefore, awayBefore - change)));
    }

    private DomainError? ValidateNewSeason(Season season, IReadOnlyCollection<TeamId> teams)
    {
        if (season == CurrentSeason)
        {
            return RatingErrors.SeasonAlreadyStarted;
        }

        if (teams.Count < 2)
        {
            return RatingErrors.TooFewTeams;
        }

        return teams.Distinct().Count() != teams.Count ? RatingErrors.DuplicateTeams : null;
    }

    private Dictionary<TeamId, double> CarryOverTo(IReadOnlyCollection<TeamId> teams)
    {
        // 平均は、チームの ID の順に足す。浮動小数点の足し算は順番で最後の桁が変わることがあるので、
        // 表に入れた順番（渡された一覧や DB から読んだ順番）によらず、毎回まったく同じ値になるようにする
        var mean = _ratings.OrderBy(pair => pair.Key.Value).Select(pair => pair.Value).Average();
        var carriedOver = _ratings.ToDictionary(
            pair => pair.Key,
            pair => mean + (_settings.CarryOver * (pair.Value - mean)));
        var promotedRating = carriedOver.Values.Order().Take(BottomTeamsForPromoted).Average();

        return teams.ToDictionary(
            team => team,
            team => carriedOver.TryGetValue(team, out var rating) ? rating : promotedRating);
    }

    private DomainError? ValidateMatch(Match match)
    {
        if (CurrentSeason is null)
        {
            return RatingErrors.NoSeasonStarted;
        }

        if (match.Season != CurrentSeason)
        {
            return RatingErrors.SeasonMismatch;
        }

        if (match.Score is null)
        {
            return RatingErrors.MatchNotFinished;
        }

        if (!_ratings.ContainsKey(match.HomeTeamId) || !_ratings.ContainsKey(match.AwayTeamId))
        {
            return RatingErrors.TeamNotInSeason;
        }

        return LastKickoff is not null && match.Kickoff.Utc < LastKickoff.Utc ? RatingErrors.OutOfOrder : null;
    }
}
