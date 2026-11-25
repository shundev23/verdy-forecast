using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Predictions;

/// <summary>
/// ％を入れなかった予想を、確率に直すルール（作業指示書 D8）。
/// 結果の順番（勝ち＞引き分け＞負け）を考えて、選んだ結果の隣に多く配る。どの結果にも 0% を付けない。
/// </summary>
public static class PercentageConversion
{
    private static readonly OutcomePercentages _winV1 = OutcomePercentages.FromValid(win: 60, draw: 25, loss: 15);
    private static readonly OutcomePercentages _drawV1 = OutcomePercentages.FromValid(win: 20, draw: 60, loss: 20);
    private static readonly OutcomePercentages _lossV1 = OutcomePercentages.FromValid(win: 15, draw: 25, loss: 60);

    /// <summary>ルール v1 で、3択を確率に直す。</summary>
    public static OutcomePercentages FromChoiceV1(TeamOutcome choice) => choice switch
    {
        TeamOutcome.Win => _winV1,
        TeamOutcome.Draw => _drawV1,
        TeamOutcome.Loss => _lossV1,
        _ => throw new ArgumentOutOfRangeException(nameof(choice), choice, "定義されていない結果です。"),
    };
}
