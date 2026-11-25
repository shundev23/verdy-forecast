using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Predictions;

/// <summary>自分の予想に関する失敗（作業指示書 D8）。</summary>
public static class PredictionErrors
{
    public static readonly DomainError PercentageBelowMinimum = new("prediction.percentageBelowMinimum", "それぞれの％は1以上にしてください（0%は付けられません）。");

    public static readonly DomainError PercentagesNotTotal = new("prediction.percentagesNotTotal", "勝ち・分け・負けの％の合計を100にしてください。");

    public static readonly DomainError ScoreDoesNotMatchChoice = new("prediction.scoreDoesNotMatchChoice", "予想スコアが、選んだ結果（勝ち・分け・負け）と合っていません。");

    public static readonly DomainError ChoiceNotHighest = new("prediction.choiceNotHighest", "選んだ結果の％が、いちばん高くなるようにしてください（同じ値は可）。");

    /// <summary>キックオフ以降、延期中、終了後。API では 409 にする（作業指示書 5-5）。</summary>
    public static readonly DomainError Locked = new("prediction.locked", "キックオフの時刻を過ぎたか、延期中または終了した試合なので、予想は入れたり変えたり消したりできません。");

    public static readonly DomainError NotVerdyMatch = new("prediction.notVerdyMatch", "ヴェルディが出ない試合には、予想を入れられません。");
}
