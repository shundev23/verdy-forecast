using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Evaluation;

/// <summary>答え合わせ（評価指標）に関する失敗。</summary>
public static class EvaluationErrors
{
    public static readonly DomainError MatchNotFinished = new("evaluation.matchNotFinished", "終わっていない試合は、答え合わせができません。");

    public static readonly DomainError NotVerdyMatch = new("evaluation.notVerdyMatch", "ヴェルディが出ない試合です。");

    public static readonly DomainError NoPastResults = new("evaluation.noPastResults", "リーグ平均の頻度を出すには、過去の試合の結果が1件以上必要です。");

    public static readonly DomainError NoEvaluations = new("evaluation.noEvaluations", "平均を出すには、答え合わせの結果が1件以上必要です。");
}
