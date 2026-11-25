using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Forecasting;

public class GoalModelFitterTests
{
    private const int Seed = 20261126;
    private static readonly DateTimeOffset _firstKickoff = new(2024, 2, 1, 5, 0, 0, TimeSpan.Zero);

    // 元の係数: ホームの平均 1.5点、アウェイの平均 1.1点、Elo 100点の差で期待得点が e^0.15 ≒ 1.16 倍
    private static readonly GoalModelCoefficients _truth =
        GoalModelCoefficients.Create(Math.Log(1.5), Math.Log(1.1), 0.15).Value;

    [Fact]
    public void Fit_SyntheticData_RecoversOriginalCoefficients()
    {
        var observations = SyntheticMatches.Generate(_truth, count: 2000, Seed, _firstKickoff);

        var fitted = GoalModelFitter.Fit(observations, CutoffAfter(observations)).ShouldSucceed();

        // 2,000試合での推定のぶれ（標準誤差）は 0.02 程度なので、その2倍以上の幅で確かめる
        fitted.HomeIntercept.ShouldBe(_truth.HomeIntercept, 0.05);
        fitted.AwayIntercept.ShouldBe(_truth.AwayIntercept, 0.05);
        fitted.Slope.ShouldBe(_truth.Slope, 0.05);
    }

    [Fact]
    public void Fit_AtOptimum_PredictedGoalsEqualObservedGoals()
    {
        // ポアソン回帰の最尤推定では、切片がある限り、予測した得点の合計と実際の得点の合計が一致する
        var observations = SyntheticMatches.Generate(_truth, count: 500, Seed, _firstKickoff);

        var fitted = GoalModelFitter.Fit(observations, CutoffAfter(observations)).ShouldSucceed();

        var predictedHome = observations.Sum(o => fitted.ExpectedGoalsFor(o.HomeRatingBefore, o.AwayRatingBefore).Value.Home);
        var predictedAway = observations.Sum(o => fitted.ExpectedGoalsFor(o.HomeRatingBefore, o.AwayRatingBefore).Value.Away);
        predictedHome.ShouldBe(observations.Sum(o => o.Score.Home), 1e-6);
        predictedAway.ShouldBe(observations.Sum(o => o.Score.Away), 1e-6);
    }

    [Fact]
    public void Fit_SameDataTwice_ReturnsSameCoefficients()
    {
        var observations = SyntheticMatches.Generate(_truth, count: 300, Seed, _firstKickoff);

        var first = GoalModelFitter.Fit(observations, CutoffAfter(observations)).ShouldSucceed();
        var second = GoalModelFitter.Fit(observations, CutoffAfter(observations)).ShouldSucceed();

        second.ShouldBe(first);
    }

    [Fact]
    public void Fit_LastMatchOneSecondBeforeCutoff_Succeeds()
    {
        var observations = SyntheticMatches.Generate(_truth, count: 300, Seed, _firstKickoff);
        var cutoff = KickoffTime.From(observations[^1].Kickoff.Utc.AddSeconds(1));

        GoalModelFitter.Fit(observations, cutoff).ShouldSucceed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Fit_MatchAtOrAfterCutoff_FailsWithDataAfterCutoff(int secondsFromLastKickoff)
    {
        var observations = SyntheticMatches.Generate(_truth, count: 300, Seed, _firstKickoff);
        var cutoff = KickoffTime.From(observations[^1].Kickoff.Utc.AddSeconds(secondsFromLastKickoff));

        GoalModelFitter.Fit(observations, cutoff).ShouldFailWith(ForecastErrors.DataAfterCutoff);
    }

    [Fact]
    public void Fit_ThirtyMatches_Succeeds()
    {
        var observations = SyntheticMatches.Generate(_truth, count: GoalModelFitter.MinObservations, Seed, _firstKickoff);

        GoalModelFitter.Fit(observations, CutoffAfter(observations)).ShouldSucceed();
    }

    [Fact]
    public void Fit_TwentyNineMatches_FailsWithTooFewObservations()
    {
        var observations = SyntheticMatches.Generate(_truth, count: GoalModelFitter.MinObservations - 1, Seed, _firstKickoff);

        GoalModelFitter.Fit(observations, CutoffAfter(observations)).ShouldFailWith(ForecastErrors.TooFewObservations);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Fit_OneSideNeverScores_FailsWithNoGoals(bool homeNeverScores)
    {
        var observations = Enumerable.Range(0, 40)
            .Select(i => Observation(i, 1500 + (i * 5), 1500, homeNeverScores ? Make.ScoreOf(0, i % 3) : Make.ScoreOf(i % 3, 0)))
            .ToList();

        GoalModelFitter.Fit(observations, CutoffAfter(observations)).ShouldFailWith(ForecastErrors.NoGoals);
    }

    [Fact]
    public void Fit_AllRatingDifferencesSame_FailsWithNotEstimable()
    {
        var observations = Enumerable.Range(0, 40)
            .Select(i => Observation(i, 1550, 1500, Make.ScoreOf(i % 3, (i + 1) % 2)))
            .ToList();

        GoalModelFitter.Fit(observations, CutoffAfter(observations)).ShouldFailWith(ForecastErrors.NotEstimable);
    }

    [Fact]
    public void GoalObservation_RatingDifference_IsHundredthOfDifference() =>
        Observation(0, 1650, 1500, Make.ScoreOf(1, 0)).RatingDifference.ShouldBe(1.5, 1e-12);

    [Theory]
    [InlineData(double.NaN, 1500)]
    [InlineData(1500, double.NegativeInfinity)]
    public void GoalObservation_NonFiniteRating_Fails(double homeRating, double awayRating) =>
        GoalObservation.Create(KickoffTime.From(_firstKickoff), homeRating, awayRating, Make.ScoreOf(1, 0))
            .ShouldFailWith(ForecastErrors.InvalidRating);

    private static GoalObservation Observation(int index, double homeRating, double awayRating, Score score) =>
        GoalObservation.Create(KickoffTime.From(_firstKickoff.AddHours(index)), homeRating, awayRating, score).ShouldSucceed();

    private static KickoffTime CutoffAfter(IReadOnlyList<GoalObservation> observations) =>
        KickoffTime.From(observations[^1].Kickoff.Utc.AddDays(1));
}
