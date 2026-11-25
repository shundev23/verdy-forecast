using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Ratings;

/// <summary>Elo レーティングに関する失敗。</summary>
public static class RatingErrors
{
    public static readonly DomainError InvalidK = new("elo.invalidK", "K は 0 より大きく 100 以下にしてください。");

    public static readonly DomainError InvalidHomeAdvantage = new("elo.invalidHomeAdvantage", "ホームの補正は 0〜400 にしてください。");

    public static readonly DomainError InvalidCarryOver = new("elo.invalidCarryOver", "シーズンの区切りで残す割合（c）は 0〜1 にしてください。");

    public static readonly DomainError TooFewTeams = new("elo.tooFewTeams", "シーズンには2チーム以上が必要です。");

    public static readonly DomainError DuplicateTeams = new("elo.duplicateTeams", "同じチームが2回入っています。");

    public static readonly DomainError SeasonAlreadyStarted = new("elo.seasonAlreadyStarted", "そのシーズンは、すでに始めています。");

    public static readonly DomainError NoSeasonStarted = new("elo.noSeasonStarted", "先にシーズンを始めてください。");

    public static readonly DomainError SeasonMismatch = new("elo.seasonMismatch", "今のシーズンの試合ではありません。");

    public static readonly DomainError MatchNotFinished = new("elo.matchNotFinished", "終わっていない試合は、レーティングに反映できません。");

    public static readonly DomainError TeamNotInSeason = new("elo.teamNotInSeason", "今のシーズンにいないチームの試合です。");

    public static readonly DomainError OutOfOrder = new("elo.outOfOrder", "試合はキックオフの順に反映してください（前に反映した試合より早い試合です）。");
}
