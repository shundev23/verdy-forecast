using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Matches;

/// <summary>あるチームから見たスコア（自分の予想スコアは、ヴェルディから見たこの向き）。</summary>
public sealed record TeamScore
{
    private TeamScore(int goalsFor, int goalsAgainst)
    {
        GoalsFor = goalsFor;
        GoalsAgainst = goalsAgainst;
    }

    /// <summary>そのチームの得点。</summary>
    public int GoalsFor { get; }

    /// <summary>相手の得点。</summary>
    public int GoalsAgainst { get; }

    public TeamOutcome Outcome => GoalsFor.CompareTo(GoalsAgainst) switch
    {
        > 0 => TeamOutcome.Win,
        0 => TeamOutcome.Draw,
        _ => TeamOutcome.Loss,
    };

    public static Result<TeamScore> Create(int goalsFor, int goalsAgainst) =>
        goalsFor < 0 || goalsAgainst < 0
            ? Result.Failure<TeamScore>(MatchErrors.NegativeGoals)
            : Result.Success(new TeamScore(goalsFor, goalsAgainst));

    /// <summary>そのチームが指定した側だったときの、ホームから見たスコアに直す。</summary>
    public Score ToScore(Side side)
    {
        Guard.Defined(side);
        return side == Side.Home ? Score.FromValid(GoalsFor, GoalsAgainst) : Score.FromValid(GoalsAgainst, GoalsFor);
    }

    // すでに検証済みの値から作る（向きを変えるときだけ使う）
    internal static TeamScore FromValid(int goalsFor, int goalsAgainst) => new(goalsFor, goalsAgainst);
}
