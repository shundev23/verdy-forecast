using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Teams;

namespace VerdyForecast.Domain.Matches;

/// <summary>J1 の試合。</summary>
public sealed class Match
{
    private Match(Season season, KickoffTime kickoff, TeamId homeTeamId, TeamId awayTeamId)
    {
        Season = season;
        Kickoff = kickoff;
        HomeTeamId = homeTeamId;
        AwayTeamId = awayTeamId;
        Status = MatchStatus.Scheduled;
    }

    public MatchId Id { get; private set; }

    public Season Season { get; private set; }

    public KickoffTime Kickoff { get; private set; }

    public TeamId HomeTeamId { get; private set; }

    public TeamId AwayTeamId { get; private set; }

    public MatchStatus Status { get; private set; }

    /// <summary>90分のスコア（PK 戦の結果は含めない。作業指示書 D4）。終わっていない試合では null。</summary>
    public Score? Score { get; private set; }

    public static Result<Match> Schedule(Season season, KickoffTime kickoff, TeamId homeTeamId, TeamId awayTeamId)
    {
        ArgumentNullException.ThrowIfNull(season);
        ArgumentNullException.ThrowIfNull(kickoff);
        if (homeTeamId == awayTeamId)
        {
            return Result.Failure<Match>(MatchErrors.SameTeams);
        }

        return Result.Success(new Match(season, kickoff, homeTeamId, awayTeamId));
    }

    /// <summary>指定したチームが、この試合のどちらの側か。出ていないチームなら null。</summary>
    public Side? SideOf(TeamId teamId)
    {
        if (teamId == HomeTeamId)
        {
            return Side.Home;
        }

        return teamId == AwayTeamId ? Side.Away : null;
    }

    /// <summary>延期にする。新しい日時が決まったら <see cref="Reschedule"/> を呼ぶ。</summary>
    public Result Postpone()
    {
        if (Status == MatchStatus.Finished)
        {
            return Result.Failure(MatchErrors.AlreadyFinished);
        }

        Status = MatchStatus.Postponed;
        return Result.Success();
    }

    /// <summary>キックオフの日時を決め直す（延期のあとや、日時の変更）。</summary>
    public Result Reschedule(KickoffTime kickoff)
    {
        ArgumentNullException.ThrowIfNull(kickoff);
        if (Status == MatchStatus.Finished)
        {
            return Result.Failure(MatchErrors.AlreadyFinished);
        }

        Kickoff = kickoff;
        Status = MatchStatus.Scheduled;
        return Result.Success();
    }

    /// <summary>90分のスコアを記録する。終わった試合でも、取得元の訂正に合わせて記録し直せる。</summary>
    public Result RecordResult(Score score)
    {
        ArgumentNullException.ThrowIfNull(score);
        if (Status == MatchStatus.Postponed)
        {
            return Result.Failure(MatchErrors.Postponed);
        }

        Score = score;
        Status = MatchStatus.Finished;
        return Result.Success();
    }
}
