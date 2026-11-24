using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Matches;

/// <summary>
/// ホームから見たスコア（90分）。PK 戦の結果は含めない（作業指示書 D4）。
/// 上限は作業指示書の D10（3-3）で決めて、入力の検証で確かめる。
/// </summary>
public sealed record Score
{
    private Score(int home, int away)
    {
        Home = home;
        Away = away;
    }

    public int Home { get; }

    public int Away { get; }

    public MatchOutcome Outcome => Home.CompareTo(Away) switch
    {
        > 0 => MatchOutcome.HomeWin,
        0 => MatchOutcome.Draw,
        _ => MatchOutcome.AwayWin,
    };

    public static Result<Score> Create(int home, int away) =>
        home < 0 || away < 0
            ? Result.Failure<Score>(MatchErrors.NegativeGoals)
            : Result.Success(new Score(home, away));

    /// <summary>指定した側のチームから見たスコアに直す。</summary>
    public TeamScore ToTeamScore(Side side)
    {
        Guard.Defined(side);
        return side == Side.Home ? TeamScore.FromValid(Home, Away) : TeamScore.FromValid(Away, Home);
    }

    // すでに検証済みの値から作る（向きを変えるときだけ使う）
    internal static Score FromValid(int home, int away) => new(home, away);
}
