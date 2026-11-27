using Microsoft.Extensions.Time.Testing;
using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Ratings;
using VerdyForecast.Domain.Tests.TestSupport;
using static VerdyForecast.Domain.Tests.TestSupport.ForecastScenario;

namespace VerdyForecast.Domain.Tests.Forecasting;

public class MatchForecasterTests
{
    // 過去の試合を検証するとき（ウォークフォワード）の「今」
    private static readonly DateTimeOffset _backtestNow = new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Backtest_RecordsMatchModelAndTimes()
    {
        var scenario = ForecastScenario.Create();

        var forecast = Forecast(scenario, _backtestNow);

        forecast.MatchId.ShouldBe(scenario.Target.Id);
        forecast.ModelVersionName.ShouldBe(Make.NameOf("test-v1"));
        forecast.CreatedAt.ShouldBe(_backtestNow);
        forecast.DataAsOf.ShouldBe(TargetKickoff.AddHours(-3));
        forecast.DataAsOf.ShouldBeLessThan(scenario.Target.Kickoff.Utc);
    }

    [Fact]
    public void Create_EarlierThanThreeHoursBeforeKickoff_DataAsOfIsCreatedAt()
    {
        var createdAt = TargetKickoff.AddDays(-2);

        Forecast(ForecastScenario.Create(), createdAt).DataAsOf.ShouldBe(createdAt);
    }

    [Fact]
    public void Create_Ratings_AreReplayOfFinishedMatchesUpToDataAsOf()
    {
        var scenario = ForecastScenario.Create();

        var forecast = Forecast(scenario, _backtestNow);

        // 手で計算し直す: 2024年の6試合 → 2025年を始める → 2025年の終わった4試合
        var table = RatingTable.Create(EloSettings.Default);
        table.StartSeason(Make.SeasonOf("2024"), [Team1, Team2, Team3, Team5]).ShouldSucceed();
        scenario.History.Take(6).ToList().ForEach(m => table.Apply(m).ShouldSucceed());
        table.StartSeason(Make.SeasonOf("2025"), [Team1, Team2, Team3, Team4]).ShouldSucceed();
        scenario.History.Skip(6).Take(4).ToList().ForEach(m => table.Apply(m).ShouldSucceed());
        forecast.HomeRatingBefore.ShouldBe(table.Ratings[Team1], 1e-9);
        forecast.AwayRatingBefore.ShouldBe(table.Ratings[Team3], 1e-9);
    }

    [Fact]
    public void Create_ExpectedGoalsAndDistribution_FollowCoefficients()
    {
        var scenario = ForecastScenario.Create();
        var model = Model();

        var forecast = MatchForecaster.Create(scenario.Target, scenario.History, model, new FakeTimeProvider(_backtestNow)).ShouldSucceed();

        var expected = model.Coefficients.ExpectedGoalsFor(forecast.HomeRatingBefore, forecast.AwayRatingBefore).ShouldSucceed();
        forecast.ExpectedGoals.ShouldBe(expected);
        forecast.Outcomes.ShouldBe(ScoreDistribution.FromExpectedGoals(expected).Outcomes);
        forecast.MostLikelyScore.ShouldBe(forecast.Distribution.MostLikely(1)[0].Score);
        forecast.Distribution.All.Sum(s => s.Probability).ShouldBe(1.0, 1e-12);
    }

    [Fact]
    public void Create_MatchStartedExactlyThreeHoursBeforeKickoff_IsUsed()
    {
        var baseline = Forecast(ForecastScenario.Create(), _backtestNow);
        var scenario = ForecastScenario.Create();
        scenario.AddFinished("2025", TargetKickoff.AddHours(-3), Team3, Team2, 3, 0);

        var forecast = Forecast(scenario, _backtestNow);

        // チーム3（アウェイ）が勝った試合が材料に入るので、試合前の Elo が上がる
        forecast.AwayRatingBefore.ShouldBeGreaterThan(baseline.AwayRatingBefore);
    }

    [Fact]
    public void Create_MatchStartedLessThanThreeHoursBeforeKickoff_IsNotUsed()
    {
        var baseline = Forecast(ForecastScenario.Create(), _backtestNow);
        var scenario = ForecastScenario.Create();
        scenario.AddFinished("2025", TargetKickoff.AddHours(-3).AddSeconds(1), Team3, Team2, 3, 0);

        var forecast = Forecast(scenario, _backtestNow);

        forecast.HomeRatingBefore.ShouldBe(baseline.HomeRatingBefore);
        forecast.AwayRatingBefore.ShouldBe(baseline.AwayRatingBefore);
    }

    [Fact]
    public void Create_MatchStartedAfterCreatedAt_IsNotUsed()
    {
        var createdAt = TargetKickoff.AddDays(-2);
        var baseline = Forecast(ForecastScenario.Create(), createdAt);
        var scenario = ForecastScenario.Create();
        scenario.AddFinished("2025", TargetKickoff.AddDays(-1), Team1, Team2, 4, 0);

        var forecast = Forecast(scenario, createdAt);

        forecast.HomeRatingBefore.ShouldBe(baseline.HomeRatingBefore);
        forecast.AwayRatingBefore.ShouldBe(baseline.AwayRatingBefore);
    }

    [Fact]
    public void Create_HistoryInAnyOrder_GivesExactlySameForecast()
    {
        var scenario = ForecastScenario.Create();
        var expected = Forecast(scenario, _backtestNow);
        var reversed = Enumerable.Reverse(scenario.History).ToList();

        var forecast = MatchForecaster.Create(scenario.Target, reversed, Model(), new FakeTimeProvider(_backtestNow)).ShouldSucceed();

        forecast.HomeRatingBefore.ShouldBe(expected.HomeRatingBefore);
        forecast.AwayRatingBefore.ShouldBe(expected.AwayRatingBefore);
        forecast.Outcomes.ShouldBe(expected.Outcomes);
    }

    [Fact]
    public void Create_TargetNotInHistory_GivesSameForecast()
    {
        var scenario = ForecastScenario.Create();
        var expected = Forecast(scenario, _backtestNow);
        var withoutTarget = scenario.History.Where(m => m.Id != scenario.Target.Id).ToList();

        var forecast = MatchForecaster.Create(scenario.Target, withoutTarget, Model(), new FakeTimeProvider(_backtestNow)).ShouldSucceed();

        forecast.HomeRatingBefore.ShouldBe(expected.HomeRatingBefore);
        forecast.AwayRatingBefore.ShouldBe(expected.AwayRatingBefore);
    }

    [Fact]
    public void Create_FirstMatchOfFirstSeason_UsesInitialRatings()
    {
        var target = Match.Schedule(Make.SeasonOf("2025"), KickoffTime.From(SeasonStart2025), Team1, Team2).ShouldSucceed().WithId(1);

        // 係数の締め切りがキックオフちょうど（その試合より前のデータだけで推定）なら作れる
        var forecast = MatchForecaster.Create(target, [target], Model(SeasonStart2025), new FakeTimeProvider(_backtestNow)).ShouldSucceed();

        forecast.HomeRatingBefore.ShouldBe(Elo.InitialRating);
        forecast.AwayRatingBefore.ShouldBe(Elo.InitialRating);
        forecast.ExpectedGoals.Home.ShouldBe(Math.Exp(0.3365), 1e-12);
        forecast.ExpectedGoals.Away.ShouldBe(1.1, 1e-12);
    }

    [Fact]
    public void Create_EmptyHistory_UsesInitialRatings()
    {
        var target = Match.Schedule(Make.SeasonOf("2025"), KickoffTime.From(SeasonStart2025), Team1, Team2).ShouldSucceed().WithId(1);

        // 試合のデータが0件でも、予想する試合のシーズンとチームから計算できる
        var forecast = MatchForecaster.Create(target, [], Model(SeasonStart2025), new FakeTimeProvider(_backtestNow)).ShouldSucceed();

        forecast.HomeRatingBefore.ShouldBe(Elo.InitialRating);
        forecast.AwayRatingBefore.ShouldBe(Elo.InitialRating);
    }

    [Fact]
    public void Create_SeasonOpenerNotInHistory_UsesCarriedOverAndPromotedRatings()
    {
        var scenario = ForecastScenario.Create();
        var lastSeasonOnly = scenario.History.Take(6).ToList();
        // 2025年の開幕戦（チーム4 は昇格チーム）。history には2025年の試合が1つもない
        var opener = Match.Schedule(Make.SeasonOf("2025"), KickoffTime.From(SeasonStart2025), Team3, Team4).ShouldSucceed().WithId(100);

        var forecast = MatchForecaster.Create(opener, lastSeasonOnly, Model(), new FakeTimeProvider(_backtestNow)).ShouldSucceed();

        // 手で計算し直す: 2024年の6試合 → 2025年を始める（チーム3 は引き戻し、チーム4 は下位3チームの平均）
        var table = RatingTable.Create(EloSettings.Default);
        table.StartSeason(Make.SeasonOf("2024"), [Team1, Team2, Team3, Team5]).ShouldSucceed();
        lastSeasonOnly.ForEach(m => table.Apply(m).ShouldSucceed());
        table.StartSeason(Make.SeasonOf("2025"), [Team3, Team4]).ShouldSucceed();
        forecast.HomeRatingBefore.ShouldBe(table.Ratings[Team3]);
        forecast.AwayRatingBefore.ShouldBe(table.Ratings[Team4]);
        forecast.AwayRatingBefore.ShouldBeLessThan(Elo.InitialRating);
    }

    [Fact]
    public void Create_CoefficientsCutoffAfterKickoff_Fails()
    {
        var scenario = ForecastScenario.Create();

        MatchForecaster.Create(scenario.Target, scenario.History, Model(TargetKickoff.AddSeconds(1)), new FakeTimeProvider(_backtestNow))
            .ShouldFailWith(ForecastErrors.CoefficientsAfterKickoff);
    }

    [Fact]
    public void Create_PostponedTarget_Fails()
    {
        var scenario = ForecastScenario.Create();
        scenario.Target.Postpone().ShouldSucceed();

        MatchForecaster.Create(scenario.Target, scenario.History, Model(), new FakeTimeProvider(_backtestNow))
            .ShouldFailWith(ForecastErrors.TargetPostponed);
    }

    [Fact]
    public void Create_TooLargeExpectedGoals_Fails()
    {
        var scenario = ForecastScenario.Create();

        MatchForecaster.Create(scenario.Target, scenario.History, Model(homeIntercept: 3.0), new FakeTimeProvider(_backtestNow))
            .ShouldFailWith(ForecastErrors.InvalidExpectedGoals);
    }

    private static Forecast Forecast(ForecastScenario scenario, DateTimeOffset now) =>
        MatchForecaster.Create(scenario.Target, scenario.History, Model(), new FakeTimeProvider(now)).ShouldSucceed();
}
