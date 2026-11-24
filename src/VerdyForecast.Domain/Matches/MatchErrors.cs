using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Matches;

/// <summary>試合に関する失敗。</summary>
public static class MatchErrors
{
    public static readonly DomainError InvalidSeason = new("season.invalid", "シーズンは「2025」か「2026-27」の形にしてください（1993年以降）。");

    public static readonly DomainError NegativeGoals = new("score.negativeGoals", "得点は0以上にしてください。");

    public static readonly DomainError SameTeams = new("match.sameTeams", "ホームとアウェイに、同じチームは指定できません。");

    public static readonly DomainError AlreadyFinished = new("match.alreadyFinished", "終わった試合は、延期や日時の変更ができません。");

    public static readonly DomainError Postponed = new("match.postponed", "延期中の試合には、結果を記録できません。先に日時を決め直してください。");
}
