using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Forecasting;

/// <summary>期待得点、スコアの分布、係数の推定、モデルの予想の作成（MatchForecaster）に関する失敗。</summary>
public static class ForecastErrors
{
    public static readonly DomainError InvalidCoefficients = new("forecast.invalidCoefficients", "期待得点の係数が数になっていません。");

    public static readonly DomainError InvalidRating = new("forecast.invalidRating", "Elo レーティングが数になっていません。");

    public static readonly DomainError InvalidExpectedGoals = new("forecast.invalidExpectedGoals", "期待得点は 0 より大きく 5 以下にしてください。");

    public static readonly DomainError InvalidProbabilities = new("forecast.invalidProbabilities", "確率は、それぞれ 0〜1 で、合計が 1 になるようにしてください。");

    public static readonly DomainError DataAfterCutoff = new("forecast.dataAfterCutoff", "データの締め切り以降の試合が混ざっています（データリーケージの防止）。");

    public static readonly DomainError TooFewObservations = new("forecast.tooFewObservations", "係数を推定するには、30試合以上が必要です。");

    public static readonly DomainError NoGoals = new("forecast.noGoals", "ホームかアウェイの得点がすべて0なので、係数を推定できません。");

    public static readonly DomainError NotEstimable = new("forecast.notEstimable", "Elo の差がすべて同じなので、係数を推定できません。");

    public static readonly DomainError NotConverged = new("forecast.notConverged", "係数の推定が収束しませんでした。");

    public static readonly DomainError TargetPostponed = new("forecast.targetPostponed", "延期中の試合は、キックオフの日時が決まっていないので、予想を作れません。");

    public static readonly DomainError CoefficientsAfterKickoff = new("forecast.coefficientsAfterKickoff", "期待得点の係数の推定に、キックオフより後の試合が使われている可能性があります（データリーケージの防止）。");
}
