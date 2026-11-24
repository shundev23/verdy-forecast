using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Players;
using VerdyForecast.Domain.Predictions;
using VerdyForecast.Domain.Reviews;
using VerdyForecast.Domain.Teams;

namespace VerdyForecast.Domain.Tests.TestSupport;

/// <summary>テスト用の、正しい値の作り方。値はすべて作り物。</summary>
internal static class Make
{
    public static Name NameOf(string value = "テスト") => Name.Create(value).ShouldSucceed();

    public static Season SeasonOf(string value = "2026-27") => Season.Create(value).ShouldSucceed();

    public static KickoffTime KickoffAt(int day = 28, int hour = 5) =>
        KickoffTime.From(new DateTimeOffset(2026, 11, day, hour, 0, 0, TimeSpan.Zero));

    public static Score ScoreOf(int home, int away) => Score.Create(home, away).ShouldSucceed();

    public static TeamScore TeamScoreOf(int goalsFor, int goalsAgainst) => TeamScore.Create(goalsFor, goalsAgainst).ShouldSucceed();

    public static OutcomePercentages PercentagesOf(int win, int draw, int loss) => OutcomePercentages.Create(win, draw, loss).ShouldSucceed();

    public static RatingScore RatingOf(decimal value) => RatingScore.Create(value).ShouldSucceed();

    public static ShirtNumber ShirtNumberOf(int value) => ShirtNumber.Create(value).ShouldSucceed();

    public static PlayerAppearance AppearanceOf(PlayerId playerId, int minutes = 90, int goals = 0) =>
        PlayerAppearance.Create(playerId, minutes, goals, assists: 0, yellowCards: 0, sentOff: false, ownGoals: 0).ShouldSucceed();

    public static Match ScheduledMatch(TeamId homeTeamId, TeamId awayTeamId) =>
        Match.Schedule(SeasonOf(), KickoffAt(), homeTeamId, awayTeamId).ShouldSucceed();
}
