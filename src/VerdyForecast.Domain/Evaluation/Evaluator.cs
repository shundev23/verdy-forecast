using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Predictions;
using VerdyForecast.Domain.Teams;

namespace VerdyForecast.Domain.Evaluation;

/// <summary>終わった試合1つの答え合わせ（評価手順書 9章）。</summary>
public static class Evaluator
{
    /// <summary>
    /// 自分の予想を、ヴェルディから見た向きで答え合わせする。
    /// 確率は、予想と一緒に保存した「評価に使う確率」（％がなければ D8 のルールで直した値）を使う。
    /// 的中は、3択で選んだ結果が起きたかで判定する。
    /// </summary>
    public static Result<PredictionEvaluation> EvaluateMyPrediction(MyPrediction prediction, Match match, TeamId verdyTeamId)
    {
        ArgumentNullException.ThrowIfNull(prediction);
        ArgumentNullException.ThrowIfNull(match);
        if (prediction.MatchId != match.Id)
        {
            throw new ArgumentException("この予想とは別の試合です。", nameof(match));
        }

        if (match.Score is null)
        {
            return Result.Failure<PredictionEvaluation>(EvaluationErrors.MatchNotFinished);
        }

        var verdySide = match.SideOf(verdyTeamId);
        if (verdySide is null)
        {
            return Result.Failure<PredictionEvaluation>(EvaluationErrors.NotVerdyMatch);
        }

        var actual = match.Score.Outcome.ToTeamOutcome(verdySide.Value);
        var outcome = ForecastMetrics.ForTeam(prediction.EvaluationPercentages.ToProbabilities(), actual)
            with { Hit = prediction.Choice == actual };
        var score = ForecastMetrics.CompareScores(prediction.PredictedScore, match.Score.ToTeamScore(verdySide.Value));
        return Result.Success(new PredictionEvaluation(outcome, score));
    }

    /// <summary>モデルの予想を、ホームから見た向きで答え合わせする。予想スコアは、分布でいちばん確率の高いスコア。</summary>
    public static Result<PredictionEvaluation> EvaluateModel(MatchOutcomeProbabilities probabilities, Score predictedScore, Match match)
    {
        ArgumentNullException.ThrowIfNull(probabilities);
        ArgumentNullException.ThrowIfNull(predictedScore);
        ArgumentNullException.ThrowIfNull(match);
        if (match.Score is null)
        {
            return Result.Failure<PredictionEvaluation>(EvaluationErrors.MatchNotFinished);
        }

        return Result.Success(new PredictionEvaluation(
            ForecastMetrics.ForMatch(probabilities, match.Score.Outcome),
            ForecastMetrics.CompareScores(predictedScore, match.Score)));
    }

    /// <summary>ベースライン（評価手順書 9.2）を答え合わせする。予想スコアはないので、Score は null。</summary>
    public static Result<PredictionEvaluation> EvaluateBaseline(MatchOutcomeProbabilities probabilities, Match match)
    {
        ArgumentNullException.ThrowIfNull(probabilities);
        ArgumentNullException.ThrowIfNull(match);
        if (match.Score is null)
        {
            return Result.Failure<PredictionEvaluation>(EvaluationErrors.MatchNotFinished);
        }

        return Result.Success(new PredictionEvaluation(ForecastMetrics.ForMatch(probabilities, match.Score.Outcome), Score: null));
    }
}
