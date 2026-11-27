using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Ratings;
using VerdyForecast.Domain.Teams;

namespace VerdyForecast.Domain.Tests.TestSupport;

/// <summary>
/// モデルの予想のテスト用の、作り物の2シーズン分の日程と結果。
/// 2024年: チーム1、2、3、5。2025年: チーム1、2、3、4（4 が昇格、5 が降格）。
/// 予想する試合は、2025年3月15日 05:00（UTC）のチーム1（ホーム）対チーム3（アウェイ）。
/// </summary>
internal sealed class ForecastScenario
{
    public static readonly TeamId Team1 = new(1);
    public static readonly TeamId Team2 = new(2);
    public static readonly TeamId Team3 = new(3);
    public static readonly TeamId Team4 = new(4);
    public static readonly TeamId Team5 = new(5);

    public static readonly DateTimeOffset TargetKickoff = new(2025, 3, 15, 5, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset SeasonStart2025 = new(2025, 3, 1, 5, 0, 0, TimeSpan.Zero);

    private int _nextId = 1;

    private ForecastScenario()
    {
    }

    /// <summary>すべての試合（予想する試合と、そのあとの試合も含む）。</summary>
    public List<Match> History { get; } = [];

    /// <summary>予想する試合。</summary>
    public Match Target { get; private set; } = null!;

    /// <summary>
    /// 予想する試合と同じ日の、2時間前に始まる試合（結果が出ていないとみなすので、材料にしない）。
    /// 現実には同じチームが2時間前に試合をすることはないが、結果が漏れたら予想が変わるよう、予想する試合のチーム（チーム3）を入れる。
    /// </summary>
    public Match SameDayAfterCutoff { get; private set; } = null!;

    /// <summary>キックオフより前（材料の時点より前）にあるが、まだ結果のない試合。</summary>
    public Match UnfinishedBeforeCutoff { get; private set; } = null!;

    /// <summary>予想する試合より後の試合。結果が漏れたら予想が変わるよう、どれも予想する試合のチームが出る。</summary>
    public List<Match> Later { get; } = [];

    public static ForecastScenario Create()
    {
        var scenario = new ForecastScenario();
        scenario.AddFinished("2024", new(2024, 3, 2, 5, 0, 0, TimeSpan.Zero), Team1, Team2, 2, 0);
        scenario.AddFinished("2024", new(2024, 3, 2, 7, 0, 0, TimeSpan.Zero), Team3, Team5, 1, 0);
        scenario.AddFinished("2024", new(2024, 3, 9, 5, 0, 0, TimeSpan.Zero), Team2, Team3, 1, 1);
        scenario.AddFinished("2024", new(2024, 3, 9, 7, 0, 0, TimeSpan.Zero), Team5, Team1, 0, 3);
        scenario.AddFinished("2024", new(2024, 3, 16, 5, 0, 0, TimeSpan.Zero), Team1, Team3, 1, 0);
        scenario.AddFinished("2024", new(2024, 3, 16, 7, 0, 0, TimeSpan.Zero), Team2, Team5, 2, 1);

        scenario.AddFinished("2025", SeasonStart2025, Team1, Team2, 1, 1);
        scenario.AddFinished("2025", new(2025, 3, 1, 7, 0, 0, TimeSpan.Zero), Team3, Team4, 0, 2);
        scenario.AddFinished("2025", new(2025, 3, 8, 5, 0, 0, TimeSpan.Zero), Team2, Team3, 2, 0);
        scenario.AddFinished("2025", new(2025, 3, 8, 7, 0, 0, TimeSpan.Zero), Team4, Team1, 1, 2);
        scenario.UnfinishedBeforeCutoff = scenario.AddScheduled("2025", new(2025, 3, 12, 10, 0, 0, TimeSpan.Zero), Team3, Team2);
        scenario.SameDayAfterCutoff = scenario.AddScheduled("2025", TargetKickoff.AddHours(-2), Team3, Team2);
        scenario.Target = scenario.AddScheduled("2025", TargetKickoff, Team1, Team3);
        scenario.Later.Add(scenario.AddScheduled("2025", new(2025, 3, 22, 5, 0, 0, TimeSpan.Zero), Team3, Team1));
        scenario.Later.Add(scenario.AddScheduled("2025", new(2025, 3, 25, 10, 0, 0, TimeSpan.Zero), Team2, Team3));
        return scenario;
    }

    /// <summary>テスト用のモデル。係数の締め切りは、指定しなければ2025年のシーズンの開始。</summary>
    public static ModelVersion Model(DateTimeOffset? coefficientsDataCutoff = null, double homeIntercept = 0.3365) =>
        ModelVersion.Create(
            Make.NameOf("test-v1"),
            EloSettings.Default,
            GoalModelCoefficients.Create(homeIntercept, Math.Log(1.1), 0.12).ShouldSucceed(),
            KickoffTime.From(coefficientsDataCutoff ?? SeasonStart2025));

    public Match AddFinished(string season, DateTimeOffset kickoff, TeamId home, TeamId away, int homeGoals, int awayGoals)
    {
        var match = AddScheduled(season, kickoff, home, away);
        match.RecordResult(Make.ScoreOf(homeGoals, awayGoals)).ShouldSucceed();
        return match;
    }

    public Match AddScheduled(string season, DateTimeOffset kickoff, TeamId home, TeamId away)
    {
        var match = Match.Schedule(Make.SeasonOf(season), KickoffTime.From(kickoff), home, away).ShouldSucceed().WithId(_nextId++);
        History.Add(match);
        return match;
    }
}
