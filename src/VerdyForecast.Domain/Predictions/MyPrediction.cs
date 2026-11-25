using System.Diagnostics.CodeAnalysis;
using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Teams;

namespace VerdyForecast.Domain.Predictions;

/// <summary>
/// 試合前の自分の予想（ヴェルディから見た向き）。1試合に1つ。
/// 入れる・変える・消すことができるのは、試合が予定の状態で、今がキックオフより前のときだけ（作業指示書 2-3）。
/// ロックはキックオフの時刻で判定するので、ロックの瞬間に動く処理はない。
/// </summary>
public sealed class MyPrediction
{
    private MyPrediction(
        MatchId matchId,
        TeamOutcome choice,
        TeamScore predictedScore,
        OutcomePercentages? percentages,
        DateTimeOffset now)
    {
        MatchId = matchId;
        EnteredAt = now;
        Apply(choice, predictedScore, percentages, now);
    }

    public MatchId MatchId { get; private set; }

    /// <summary>3択（勝ち・分け・負け）。必須。</summary>
    public TeamOutcome Choice { get; private set; }

    /// <summary>予想スコア（ヴェルディの得点、相手の得点）。必須。</summary>
    public TeamScore PredictedScore { get; private set; }

    /// <summary>自分が入れた％（任意）。入れていなければ null。</summary>
    public OutcomePercentages? Percentages { get; private set; }

    /// <summary>
    /// 評価に使う確率。％を入れたときはその値、入れていないときは D8 のルールで直した値。
    /// 保存のたびに決め直すが、キックオフ以降は変更できないので、キックオフ時点の値がそのまま残る。
    /// </summary>
    public OutcomePercentages EvaluationPercentages { get; private set; }

    public EvaluationPercentagesSource EvaluationSource { get; private set; }

    /// <summary>最初に入れた日時（UTC）。</summary>
    public DateTimeOffset EnteredAt { get; private set; }

    /// <summary>最後に変更した日時（UTC）。キックオフより前であることを、評価のときに確かめる（評価手順書 9.4）。</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>予想を入れる。現在時刻は TimeProvider から1回だけ読む。</summary>
    public static Result<MyPrediction> Create(
        Match match,
        TeamId verdyTeamId,
        TeamOutcome choice,
        TeamScore predictedScore,
        OutcomePercentages? percentages,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        Guard.Defined(choice);
        ArgumentNullException.ThrowIfNull(predictedScore);

        if (match.SideOf(verdyTeamId) is null)
        {
            return Result.Failure<MyPrediction>(PredictionErrors.NotVerdyMatch);
        }

        var now = clock.GetUtcNow();
        var error = CheckOpen(match, now) ?? Validate(choice, predictedScore, percentages);
        if (error is not null)
        {
            return Result.Failure<MyPrediction>(error);
        }

        return Result.Success(new MyPrediction(match.Id, choice, predictedScore, percentages, now));
    }

    /// <summary>予想を変える。ロック中や、内容が正しくないときは、何も変えずに失敗を返す。</summary>
    public Result Change(
        Match match,
        TeamOutcome choice,
        TeamScore predictedScore,
        OutcomePercentages? percentages,
        TimeProvider clock)
    {
        EnsureSameMatch(match);
        ArgumentNullException.ThrowIfNull(clock);
        Guard.Defined(choice);
        ArgumentNullException.ThrowIfNull(predictedScore);

        var now = clock.GetUtcNow();
        var error = CheckOpen(match, now) ?? Validate(choice, predictedScore, percentages);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        Apply(choice, predictedScore, percentages, now);
        return Result.Success();
    }

    /// <summary>予想を消してよいか。実際に消すのは、保存を受け持つ側（Application と Infrastructure）。</summary>
    public Result CheckCanDelete(Match match, TimeProvider clock)
    {
        EnsureSameMatch(match);
        ArgumentNullException.ThrowIfNull(clock);

        var error = CheckOpen(match, clock.GetUtcNow());
        return error is null ? Result.Success() : Result.Failure(error);
    }

    private static DomainError? CheckOpen(Match match, DateTimeOffset now) =>
        match.IsOpenForPredictionsAt(now) ? null : PredictionErrors.Locked;

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

    [MemberNotNull(nameof(PredictedScore), nameof(EvaluationPercentages))]
    private void Apply(TeamOutcome choice, TeamScore predictedScore, OutcomePercentages? percentages, DateTimeOffset now)
    {
        Choice = choice;
        PredictedScore = predictedScore;
        Percentages = percentages;
        EvaluationPercentages = percentages ?? PercentageConversion.FromChoiceV1(choice);
        EvaluationSource = percentages is null
            ? EvaluationPercentagesSource.ConvertedByRuleV1
            : EvaluationPercentagesSource.Entered;
        UpdatedAt = now;
    }

    private void EnsureSameMatch(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        if (match.Id != MatchId)
        {
            throw new ArgumentException("この予想とは別の試合です。", nameof(match));
        }
    }
}
