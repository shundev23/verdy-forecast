using VerdyForecast.Domain.Forecasting;
using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Tests.TestSupport;

/// <summary>
/// 決まった係数から、作り物の試合データを作る（シード固定なので毎回同じデータになる）。
/// 係数の推定が、元の係数を推定し直せるかを確かめるために使う（作業指示書 D5）。
/// </summary>
internal static class SyntheticMatches
{
    public static List<GoalObservation> Generate(GoalModelCoefficients truth, int count, int seed, DateTimeOffset firstKickoff)
    {
        var random = new Random(seed);
        var observations = new List<GoalObservation>(count);
        for (var i = 0; i < count; i++)
        {
            // Elo の差は ±300（Δ = ±3）の範囲で一様に選ぶ
            var difference = (random.NextDouble() * 6) - 3;
            var homeRating = 1500 + (difference * 50);
            var awayRating = 1500 - (difference * 50);
            var expected = truth.ExpectedGoalsFor(homeRating, awayRating).Value;
            var score = Make.ScoreOf(SamplePoisson(random, expected.Home), SamplePoisson(random, expected.Away));
            observations.Add(GoalObservation.Create(KickoffTime.From(firstKickoff.AddHours(i)), homeRating, awayRating, score).Value);
        }

        return observations;
    }

    // Knuth の方法: 一様乱数をかけていき、e^(−λ) を下回るまでの回数がポアソン分布に従う
    private static int SamplePoisson(Random random, double lambda)
    {
        var limit = Math.Exp(-lambda);
        var count = 0;
        var product = random.NextDouble();
        while (product > limit)
        {
            count++;
            product *= random.NextDouble();
        }

        return count;
    }
}
