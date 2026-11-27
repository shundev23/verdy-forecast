using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Ratings;
using VerdyForecast.Domain.Teams;

namespace VerdyForecast.Domain.Forecasting;

/// <summary>
/// モデルの予想を作る（作業指示書 2-8）。材料は「キックオフ時点で分かっていたデータ」だけにする（データリーケージの防止）。
/// 材料にするのは、試合の日程と結果、モデルのバージョンだけ。自分の採点・タグ・メモ・予想は受け取らない
/// （v1 では自分の記録をモデルの材料にしない。依存ルールのテストでも確かめている）。
/// </summary>
public static class MatchForecaster
{
    /// <summary>試合が始まってから、結果が出ているとみなすまでの時間。これより遅く始まった試合の結果は使わない。</summary>
    public static readonly TimeSpan ResultAvailableAfter = TimeSpan.FromHours(3);

    /// <summary>
    /// 予想を作る。history には、終わった試合も、まだの試合も、あとの試合も入っていてよい（使う試合はここで選ぶ）。
    /// 使うのは、終わっていて、材料の時点（作った日時と、キックオフの3時間前の、早いほう）までに始まった試合だけ。
    /// Elo は、その試合だけで最初から計算し直す。シーズンの順番と各シーズンのチームは、日程（終わっていない試合も含む）から決める
    /// （日程はシーズンの前に発表されるので、リーケージにならない）。
    /// </summary>
    public static Result<Forecast> Create(Match target, IReadOnlyCollection<Match> history, ModelVersion model, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(clock);

        if (target.Status == MatchStatus.Postponed)
        {
            return Result.Failure<Forecast>(ForecastErrors.TargetPostponed);
        }

        // 係数は、締め切りより前の試合だけで推定している。締め切りがキックオフより後だと、この試合やそのあとの試合が混ざっているかもしれない
        if (model.CoefficientsDataCutoff.Utc > target.Kickoff.Utc)
        {
            return Result.Failure<Forecast>(ForecastErrors.CoefficientsAfterKickoff);
        }

        var createdAt = clock.GetUtcNow();
        var latestUsable = target.Kickoff.Utc - ResultAvailableAfter;
        var dataAsOf = createdAt < latestUsable ? createdAt : latestUsable;

        var ratings = RatingsBefore(target, history, model.Elo, dataAsOf);
        if (ratings.IsFailure)
        {
            return Result.Failure<Forecast>(ratings.Error);
        }

        var (homeRating, awayRating) = ratings.Value;
        var expectedGoals = model.Coefficients.ExpectedGoalsFor(homeRating, awayRating);
        if (expectedGoals.IsFailure)
        {
            return Result.Failure<Forecast>(expectedGoals.Error);
        }

        var distribution = ScoreDistribution.FromExpectedGoals(expectedGoals.Value);
        return Result.Success(new Forecast(
            target.Id,
            model.Name,
            createdAt,
            dataAsOf,
            homeRating,
            awayRating,
            expectedGoals.Value,
            distribution));
    }

    private static Result<(double Home, double Away)> RatingsBefore(
        Match target,
        IReadOnlyCollection<Match> history,
        EloSettings elo,
        DateTimeOffset dataAsOf)
    {
        // シーズンの順番（最初の試合の日時の順）と、各シーズンのチーム。
        // 予想する試合も加えるのは、history にその試合のシーズンやチームがなくても（シーズンの開幕戦など）予想を作れるようにするため。
        // チームの並び順は結果に影響しない（RatingTable が平均をチームの ID の順に足す）
        var seasons = history
            .Append(target)
            .GroupBy(m => m.Season)
            .Select(g => new SeasonSchedule(
                g.Key,
                g.Min(m => m.Kickoff.Utc),
                g.SelectMany(m => new[] { m.HomeTeamId, m.AwayTeamId }).Distinct().ToList()))
            .OrderBy(s => s.FirstKickoff)
            .ThenBy(s => s.Season.Value, StringComparer.Ordinal)
            .ToList();
        var targetSeasonIndex = seasons.FindIndex(s => s.Season == target.Season);

        // 予想する試合のシーズンまでを、順に計算し直す（あとのシーズンは使わない）
        var table = RatingTable.Create(elo);
        foreach (var season in seasons.Take(targetSeasonIndex + 1))
        {
            var started = table.StartSeason(season.Season, season.Teams);
            if (started.IsFailure)
            {
                return Result.Failure<(double, double)>(started.Error);
            }

            var playedBeforeCutoff = history
                .Where(m => m.Season == season.Season && m.Score is not null && m.Kickoff.Utc <= dataAsOf)
                .OrderBy(m => m.Kickoff.Utc)
                .ThenBy(m => m.Id.Value);
            foreach (var match in playedBeforeCutoff)
            {
                var applied = table.Apply(match);
                if (applied.IsFailure)
                {
                    return Result.Failure<(double, double)>(applied.Error);
                }
            }
        }

        return Result.Success((table.Ratings[target.HomeTeamId], table.Ratings[target.AwayTeamId]));
    }

    private sealed record SeasonSchedule(Season Season, DateTimeOffset FirstKickoff, IReadOnlyCollection<TeamId> Teams);
}
