using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Players;

namespace VerdyForecast.Domain.Reviews;

/// <summary>
/// 参考点（採点の下書き）を出すルール（作業指示書 D6、ルール v1）。
/// 使うのは、公式サイトを見て手で入れた出場の記録、名簿のポジション、ヴェルディから見たスコアだけ。
/// 足し引きはすべて 0.5 の倍数なので、丸めは要らない。
/// </summary>
public static class ReferenceScoreRule
{
    /// <summary>これより短い出場の選手には、参考点を出さない。</summary>
    public const int MinMinutesToRate = 15;

    /// <summary>無失点で加点するのに必要な出場時間（GK と DF）。</summary>
    public const int MinMinutesForCleanSheet = 60;

    /// <summary>この数以上の失点で減点する（GK と DF）。</summary>
    public const int ConcededForPenalty = 3;

    public const decimal Lowest = 4.0m;
    public const decimal Highest = 9.0m;

    private const decimal Base = 6.0m;
    private const decimal PerGoal = 1.0m;
    private const decimal PerAssist = 0.5m;
    private const decimal ForTeamResult = 0.5m;
    private const decimal ForCleanSheet = 0.5m;
    private const decimal ForConceded = 0.5m;
    private const decimal ForSendingOff = 1.0m;
    private const decimal PerOwnGoal = 1.0m;

    /// <summary>参考点を出す。出場15分未満の選手は null。</summary>
    public static RatingScore? CalculateV1(PlayerAppearance appearance, Position position, TeamScore verdyScore)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        Guard.Defined(position);
        ArgumentNullException.ThrowIfNull(verdyScore);

        if (appearance.Minutes < MinMinutesToRate)
        {
            return null;
        }

        var score = Base
            + (appearance.Goals * PerGoal)
            + (appearance.Assists * PerAssist)
            + TeamResultPoints(verdyScore.Outcome)
            + DefensivePoints(appearance, position, verdyScore.GoalsAgainst)
            - (appearance.SentOff ? ForSendingOff : 0m)
            - (appearance.OwnGoals * PerOwnGoal);

        return RatingScore.FromValid(Math.Clamp(score, Lowest, Highest));
    }

    private static decimal TeamResultPoints(TeamOutcome outcome) => outcome switch
    {
        TeamOutcome.Win => ForTeamResult,
        TeamOutcome.Loss => -ForTeamResult,
        _ => 0m,
    };

    // 守備の加点と減点は、GK と DF だけ
    private static decimal DefensivePoints(PlayerAppearance appearance, Position position, int goalsAgainst)
    {
        if (position is not (Position.Goalkeeper or Position.Defender))
        {
            return 0m;
        }

        var points = 0m;
        if (goalsAgainst == 0 && appearance.Minutes >= MinMinutesForCleanSheet)
        {
            points += ForCleanSheet;
        }

        if (goalsAgainst >= ConcededForPenalty)
        {
            points -= ForConceded;
        }

        return points;
    }
}
