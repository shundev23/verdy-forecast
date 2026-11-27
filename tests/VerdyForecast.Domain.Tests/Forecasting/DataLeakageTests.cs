using Microsoft.Extensions.Time.Testing;
using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Players;
using VerdyForecast.Domain.Reviews;
using VerdyForecast.Domain.Tests.TestSupport;
using static VerdyForecast.Domain.Tests.TestSupport.ForecastScenario;

namespace VerdyForecast.Domain.Tests.Forecasting;

/// <summary>
/// データリーケージのテスト（スキルの testing.md の手順、作業指示書 2-8）。
/// 予想に使うデータが「キックオフ時点で分かっていたもの」だけかを確かめる。
/// </summary>
public class DataLeakageTests
{
    private static readonly DateTimeOffset _backtestNow = new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Forecast_AfterAddingDataFromAfterKickoff_IsExactlyTheSame()
    {
        var scenario = ForecastScenario.Create();

        // 1. 過去のある試合について、予想を作る
        var before = Forecast(scenario);

        // 2. その試合のキックオフより後のデータを足す
        // その試合の結果
        scenario.Target.RecordResult(Make.ScoreOf(0, 4)).ShouldSucceed();
        // 同じ日の、結果が出ていないとみなす時間（キックオフの3時間前より後）に始まった試合の結果
        scenario.SameDayAfterCutoff.RecordResult(Make.ScoreOf(5, 0)).ShouldSucceed();
        // あとの試合の結果と、新しく足した、あとの試合
        scenario.Later.ForEach(m => m.RecordResult(Make.ScoreOf(3, 3)).ShouldSucceed());
        scenario.AddFinished("2025", new(2025, 3, 29, 5, 0, 0, TimeSpan.Zero), Team1, Team4, 6, 0);
        // その試合の、試合後の採点と MVP。モデルの材料の型に入らないので渡す先がない
        // （モデルの計算が採点を参照しないことは、依存ルールのテスト ModelInputTests でも確かめている）
        var review = MatchReview.Start(scenario.Target.Id);
        review.RecordAppearance(Make.AppearanceOf(new PlayerId(1), goals: 3));
        review.RateFinal(new PlayerId(1), Make.RatingOf(9.5m)).ShouldSucceed();
        review.ChooseMvp(new PlayerId(1)).ShouldSucceed();

        // 3. もう一度、同じ試合の予想を作る
        var after = Forecast(scenario);

        // 4. 1 と 3 の予想が完全に同じ
        ShouldBeExactlyTheSame(after, before);
    }

    [Fact]
    public void Forecast_AfterAddingNextSeason_IsExactlyTheSame()
    {
        var scenario = ForecastScenario.Create();
        var before = Forecast(scenario);

        // 次のシーズン（新しいチームを含む）の日程と結果を足す
        scenario.AddFinished("2026", new(2026, 2, 7, 5, 0, 0, TimeSpan.Zero), Team5, Team1, 4, 0);
        scenario.AddFinished("2026", new(2026, 2, 7, 7, 0, 0, TimeSpan.Zero), Team3, Team2, 0, 4);

        ShouldBeExactlyTheSame(Forecast(scenario), before);
    }

    [Fact]
    public void Forecast_AfterAddingDataFromBeforeKickoff_Changes()
    {
        // このテストのしかけが、本当の変化を見落とさないことの確認。キックオフより前のデータを足すと、予想は変わる
        var scenario = ForecastScenario.Create();
        var before = Forecast(scenario);

        scenario.UnfinishedBeforeCutoff.RecordResult(Make.ScoreOf(4, 0)).ShouldSucceed();

        var after = Forecast(scenario);
        after.AwayRatingBefore.ShouldBeGreaterThan(before.AwayRatingBefore);
        after.Outcomes.ShouldNotBe(before.Outcomes);
    }

    [Fact]
    public void Forecast_CreatedLaterAfterKickoff_DiffersOnlyInCreatedAt()
    {
        var scenario = ForecastScenario.Create();
        var first = Forecast(scenario);

        var later = MatchForecaster.Create(scenario.Target, scenario.History, Model(), new FakeTimeProvider(_backtestNow.AddMonths(6))).ShouldSucceed();

        later.CreatedAt.ShouldBe(_backtestNow.AddMonths(6));
        later.DataAsOf.ShouldBe(first.DataAsOf);
        later.HomeRatingBefore.ShouldBe(first.HomeRatingBefore);
        later.AwayRatingBefore.ShouldBe(first.AwayRatingBefore);
        later.Distribution.All.ShouldBe(first.Distribution.All);
    }

    private static Forecast Forecast(ForecastScenario scenario) =>
        MatchForecaster.Create(scenario.Target, scenario.History, Model(), new FakeTimeProvider(_backtestNow)).ShouldSucceed();

    private static void ShouldBeExactlyTheSame(Forecast actual, Forecast expected)
    {
        actual.MatchId.ShouldBe(expected.MatchId);
        actual.ModelVersionName.ShouldBe(expected.ModelVersionName);
        actual.CreatedAt.ShouldBe(expected.CreatedAt);
        actual.DataAsOf.ShouldBe(expected.DataAsOf);
        actual.HomeRatingBefore.ShouldBe(expected.HomeRatingBefore);
        actual.AwayRatingBefore.ShouldBe(expected.AwayRatingBefore);
        actual.ExpectedGoals.ShouldBe(expected.ExpectedGoals);
        actual.Outcomes.ShouldBe(expected.Outcomes);
        actual.MostLikelyScore.ShouldBe(expected.MostLikelyScore);
        actual.Distribution.All.ShouldBe(expected.Distribution.All);
    }
}
