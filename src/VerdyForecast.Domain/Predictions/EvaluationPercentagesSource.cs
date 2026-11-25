namespace VerdyForecast.Domain.Predictions;

/// <summary>評価に使う確率の出どころ（作業指示書 D8）。ルールを変えるときは、新しい値を足して古い値は残す。</summary>
public enum EvaluationPercentagesSource
{
    /// <summary>自分が入れた％をそのまま使う</summary>
    Entered,

    /// <summary>％を入れていないので、3択を D8 のルール v1 で確率に直した</summary>
    ConvertedByRuleV1,
}
