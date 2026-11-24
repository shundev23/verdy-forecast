namespace VerdyForecast.Domain.Matches;

/// <summary>ホームから見た結果と、あるチームから見た結果を変換する。</summary>
public static class OutcomeConversions
{
    /// <summary>ホームから見た結果を、指定した側のチームから見た結果に直す。</summary>
    public static TeamOutcome ToTeamOutcome(this MatchOutcome outcome, Side side) => (outcome, side) switch
    {
        (MatchOutcome.Draw, Side.Home or Side.Away) => TeamOutcome.Draw,
        (MatchOutcome.HomeWin, Side.Home) or (MatchOutcome.AwayWin, Side.Away) => TeamOutcome.Win,
        (MatchOutcome.HomeWin, Side.Away) or (MatchOutcome.AwayWin, Side.Home) => TeamOutcome.Loss,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), "定義されていない結果か側です。"),
    };

    /// <summary>指定した側のチームから見た結果を、ホームから見た結果に直す。</summary>
    public static MatchOutcome ToMatchOutcome(this TeamOutcome outcome, Side side) => (outcome, side) switch
    {
        (TeamOutcome.Draw, Side.Home or Side.Away) => MatchOutcome.Draw,
        (TeamOutcome.Win, Side.Home) or (TeamOutcome.Loss, Side.Away) => MatchOutcome.HomeWin,
        (TeamOutcome.Win, Side.Away) or (TeamOutcome.Loss, Side.Home) => MatchOutcome.AwayWin,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), "定義されていない結果か側です。"),
    };
}
