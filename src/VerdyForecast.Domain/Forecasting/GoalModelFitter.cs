using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;

namespace VerdyForecast.Domain.Forecasting;

/// <summary>
/// 期待得点の係数（a(ホーム)、a(アウェイ)、b）を、過去の試合から最尤法（ニュートン法）で推定する（作業指示書 D5）。
/// 外部のライブラリは使わない。本番と検証で同じ計算を使う。
/// </summary>
public static class GoalModelFitter
{
    /// <summary>推定に必要な試合の数。係数が3つなので、少なすぎると値がぶれる。</summary>
    public const int MinObservations = 30;

    private const int MaxIterations = 100;
    private const double ConvergenceTolerance = 1e-10;
    private const double MinStepScale = 1e-10;

    /// <summary>
    /// 係数を推定する。データの締め切り（ふつうはシーズンの開始日時）以降の試合が混ざっていたら失敗にする（データリーケージの防止）。
    /// </summary>
    public static Result<GoalModelCoefficients> Fit(IReadOnlyCollection<GoalObservation> observations, KickoffTime dataCutoff)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(dataCutoff);

        var error = Validate(observations, dataCutoff);
        if (error is not null)
        {
            return Result.Failure<GoalModelCoefficients>(error);
        }

        var data = observations
            .Select(o => new DataPoint(o.RatingDifference, o.Score.Home, o.Score.Away))
            .ToArray();

        // 最初の値: 切片は平均得点の対数、傾きは 0
        double[] theta = [Math.Log(data.Average(d => d.HomeGoals)), Math.Log(data.Average(d => d.AwayGoals)), 0.0];
        var logLikelihood = LogLikelihood(data, theta);

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            var (gradient, hessian) = Derivatives(data, theta);

            // ニュートン法の一歩: H × step = −g
            var step = LinearSolver.Solve(hessian, [-gradient[0], -gradient[1], -gradient[2]]);
            if (step is null)
            {
                return Result.Failure<GoalModelCoefficients>(ForecastErrors.NotEstimable);
            }

            // 尤度が下がったら、一歩を半分にする（計算を安定させるため）
            var (next, nextLogLikelihood) = TakeStep(data, theta, step, logLikelihood);
            var change = Math.Max(Math.Abs(next[0] - theta[0]), Math.Max(Math.Abs(next[1] - theta[1]), Math.Abs(next[2] - theta[2])));
            theta = next;
            logLikelihood = nextLogLikelihood;

            if (change < ConvergenceTolerance)
            {
                return Result.Success(GoalModelCoefficients.FromValid(theta[0], theta[1], theta[2]));
            }
        }

        return Result.Failure<GoalModelCoefficients>(ForecastErrors.NotConverged);
    }

    private static DomainError? Validate(IReadOnlyCollection<GoalObservation> observations, KickoffTime dataCutoff)
    {
        if (observations.Any(o => o.Kickoff.Utc >= dataCutoff.Utc))
        {
            return ForecastErrors.DataAfterCutoff;
        }

        if (observations.Count < MinObservations)
        {
            return ForecastErrors.TooFewObservations;
        }

        // どちらかの得点がすべて0だと、切片が −∞ に向かって推定できない
        if (observations.All(o => o.Score.Home == 0) || observations.All(o => o.Score.Away == 0))
        {
            return ForecastErrors.NoGoals;
        }

        return null;
    }

    private static (double[] Next, double LogLikelihood) TakeStep(DataPoint[] data, double[] theta, double[] step, double current)
    {
        var scale = 1.0;
        while (true)
        {
            double[] next = [theta[0] + (scale * step[0]), theta[1] + (scale * step[1]), theta[2] + (scale * step[2])];
            var nextLogLikelihood = LogLikelihood(data, next);
            if (nextLogLikelihood >= current || scale < MinStepScale)
            {
                return (next, nextLogLikelihood);
            }

            scale /= 2;
        }
    }

    // 対数尤度（log y! は係数によらないので省く）
    private static double LogLikelihood(DataPoint[] data, double[] theta)
    {
        var sum = 0.0;
        foreach (var d in data)
        {
            var etaHome = theta[0] + (theta[2] * d.Difference);
            var etaAway = theta[1] - (theta[2] * d.Difference);
            sum += (d.HomeGoals * etaHome) - Math.Exp(etaHome) + (d.AwayGoals * etaAway) - Math.Exp(etaAway);
        }

        return sum;
    }

    // 対数尤度の勾配と、ヘッセ行列（2階微分。負の定値なので、ニュートン法で最大値に向かう）
    private static (double[] Gradient, double[][] Hessian) Derivatives(DataPoint[] data, double[] theta)
    {
        double gHome = 0, gAway = 0, gSlope = 0;
        double hHome = 0, hAway = 0, hHomeSlope = 0, hAwaySlope = 0, hSlope = 0;
        foreach (var d in data)
        {
            var muHome = Math.Exp(theta[0] + (theta[2] * d.Difference));
            var muAway = Math.Exp(theta[1] - (theta[2] * d.Difference));
            gHome += d.HomeGoals - muHome;
            gAway += d.AwayGoals - muAway;
            gSlope += (d.Difference * (d.HomeGoals - muHome)) - (d.Difference * (d.AwayGoals - muAway));
            hHome -= muHome;
            hAway -= muAway;
            hHomeSlope -= d.Difference * muHome;
            hAwaySlope += d.Difference * muAway;
            hSlope -= d.Difference * d.Difference * (muHome + muAway);
        }

        double[] gradient = [gHome, gAway, gSlope];
        double[][] hessian =
        [
            [hHome, 0, hHomeSlope],
            [0, hAway, hAwaySlope],
            [hHomeSlope, hAwaySlope, hSlope],
        ];
        return (gradient, hessian);
    }

    private readonly record struct DataPoint(double Difference, double HomeGoals, double AwayGoals);
}
