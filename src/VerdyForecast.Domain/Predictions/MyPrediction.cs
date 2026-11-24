using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Predictions;

/// <summary>
/// 試合前の自分の予想（ヴェルディから見た向き）。1試合に1つ。
/// キックオフでのロックと、％のない予想を確率に直す処理は、作業指示書 2-3 で足す。
/// </summary>
public sealed class MyPrediction
{
    private MyPrediction(
        MatchId matchId,
        TeamOutcome choice,
        TeamScore predictedScore,
        OutcomePercentages? percentages,
        DateTimeOffset enteredAt)
    {
        MatchId = matchId;
        Choice = choice;
        PredictedScore = predictedScore;
        Percentages = percentages;
        EnteredAt = enteredAt;
    }

    public MatchId MatchId { get; private set; }

    /// <summary>3択（勝ち・分け・負け）。必須。</summary>
    public TeamOutcome Choice { get; private set; }

    /// <summary>予想スコア（ヴェルディの得点、相手の得点）。必須。</summary>
    public TeamScore PredictedScore { get; private set; }

    /// <summary>％（任意）。入れていなければ null。</summary>
    public OutcomePercentages? Percentages { get; private set; }

    /// <summary>入力した日時（UTC）。現在時刻は、呼び出す側が TimeProvider から渡す。</summary>
    public DateTimeOffset EnteredAt { get; private set; }

    public static Result<MyPrediction> Create(
        MatchId matchId,
        TeamOutcome choice,
        TeamScore predictedScore,
        OutcomePercentages? percentages,
        DateTimeOffset enteredAt)
    {
        Guard.Defined(choice);
        ArgumentNullException.ThrowIfNull(predictedScore);

        var error = Validate(choice, predictedScore, percentages);
        return error is null
            ? Result.Success(new MyPrediction(matchId, choice, predictedScore, percentages, enteredAt.ToUniversalTime()))
            : Result.Failure<MyPrediction>(error);
    }

    private static DomainError? Validate(TeamOutcome choice, TeamScore predictedScore, OutcomePercentages? percentages)
    {
        if (predictedScore.Outcome != choice)
        {
            return PredictionErrors.ScoreDoesNotMatchChoice;
        }

        if (percentages is not null && !percentages.IsHighest(choice))
        {
            return PredictionErrors.ChoiceNotHighest;
        }

        return null;
    }
}
