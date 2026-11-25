using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Forecasting;

/// <summary>スコア1つと、その確率。</summary>
public sealed record ScoreProbability(Score Score, double Probability);

/// <summary>
/// スコアの分布（作業指示書 D5）。各チーム0〜10点のポアソン分布をかけ合わせた 11×11 の表で、合計が 1 になるように割り戻す。
/// 勝ち・分け・負けの確率は、この表を足し合わせて出す（数字が食い違わないように）。
/// </summary>
public sealed class ScoreDistribution
{
    /// <summary>計算する得点の上限（各チーム）。</summary>
    public const int MaxGoals = 10;

    private const int Size = MaxGoals + 1;

    // [ホームの得点 × 11 + アウェイの得点] の確率
    private readonly double[] _cells;

    private ScoreDistribution(double[] cells)
    {
        _cells = cells;
        Outcomes = SumOutcomes(cells);
        All = Enumerable.Range(0, cells.Length)
            .Select(index => new ScoreProbability(Score.FromValid(index / Size, index % Size), cells[index]))
            .ToList();
    }

    /// <summary>ホームから見た、勝ち・分け・負けの確率。</summary>
    public MatchOutcomeProbabilities Outcomes { get; }

    /// <summary>すべてのスコアの確率（ホームの得点、アウェイの得点の小さい順）。Forecasts に保存する材料。</summary>
    public IReadOnlyList<ScoreProbability> All { get; }

    public static ScoreDistribution FromExpectedGoals(ExpectedGoals expectedGoals)
    {
        ArgumentNullException.ThrowIfNull(expectedGoals);

        var home = PoissonProbabilities(expectedGoals.Home);
        var away = PoissonProbabilities(expectedGoals.Away);
        var cells = new double[Size * Size];
        for (var h = 0; h < Size; h++)
        {
            for (var a = 0; a < Size; a++)
            {
                cells[(h * Size) + a] = home[h] * away[a];
            }
        }

        // 10点より上を切り捨てた分だけ合計が 1 より小さくなるので、割り戻す
        var total = cells.Sum();
        for (var i = 0; i < cells.Length; i++)
        {
            cells[i] /= total;
        }

        return new ScoreDistribution(cells);
    }

    /// <summary>指定したスコア（ホームの得点、アウェイの得点。各0〜10）の確率。</summary>
    public double Probability(int homeGoals, int awayGoals)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(homeGoals);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(homeGoals, MaxGoals);
        ArgumentOutOfRangeException.ThrowIfNegative(awayGoals);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(awayGoals, MaxGoals);
        return _cells[(homeGoals * Size) + awayGoals];
    }

    /// <summary>確率の高いスコアを、高い順に指定した数だけ返す。同じ確率なら、ホームの得点、アウェイの得点の小さい順。</summary>
    public IReadOnlyList<ScoreProbability> MostLikely(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Size * Size);
        return All
            .OrderByDescending(s => s.Probability)
            .ThenBy(s => s.Score.Home)
            .ThenBy(s => s.Score.Away)
            .Take(count)
            .ToList();
    }

    // P(k) = e^(−λ) × λ^k ÷ k! を、P(k) = P(k − 1) × λ ÷ k で順に出す
    private static double[] PoissonProbabilities(double lambda)
    {
        var probabilities = new double[Size];
        probabilities[0] = Math.Exp(-lambda);
        for (var k = 1; k < Size; k++)
        {
            probabilities[k] = probabilities[k - 1] * lambda / k;
        }

        return probabilities;
    }

    private static MatchOutcomeProbabilities SumOutcomes(double[] cells)
    {
        double homeWin = 0, draw = 0, awayWin = 0;
        for (var h = 0; h < Size; h++)
        {
            for (var a = 0; a < Size; a++)
            {
                var probability = cells[(h * Size) + a];
                if (h > a)
                {
                    homeWin += probability;
                }
                else if (h == a)
                {
                    draw += probability;
                }
                else
                {
                    awayWin += probability;
                }
            }
        }

        return MatchOutcomeProbabilities.FromValid(homeWin, draw, awayWin);
    }
}
