namespace VerdyForecast.Domain.Forecasting;

/// <summary>小さな連立一次方程式 A × x = b を、ガウスの消去法（部分ピボット選択）で解く。</summary>
internal static class LinearSolver
{
    // 行列の大きさに対して、ピボットがこれより小さければ「解けない（特異）」とみなす
    private const double RelativeSingularityTolerance = 1e-10;

    /// <summary>解を返す。行列が特異（解が1つに決まらない）なら null。</summary>
    public static double[]? Solve(double[][] matrix, double[] rightHandSide)
    {
        var n = rightHandSide.Length;
        var a = matrix.Select(row => row.ToArray()).ToArray();
        var b = rightHandSide.ToArray();
        var scale = a.SelectMany(row => row).Select(Math.Abs).DefaultIfEmpty(0).Max();
        if (scale == 0)
        {
            return null;
        }

        for (var column = 0; column < n; column++)
        {
            var pivotRow = column;
            for (var row = column + 1; row < n; row++)
            {
                if (Math.Abs(a[row][column]) > Math.Abs(a[pivotRow][column]))
                {
                    pivotRow = row;
                }
            }

            if (Math.Abs(a[pivotRow][column]) <= RelativeSingularityTolerance * scale)
            {
                return null;
            }

            (a[column], a[pivotRow]) = (a[pivotRow], a[column]);
            (b[column], b[pivotRow]) = (b[pivotRow], b[column]);

            for (var row = column + 1; row < n; row++)
            {
                var factor = a[row][column] / a[column][column];
                for (var k = column; k < n; k++)
                {
                    a[row][k] -= factor * a[column][k];
                }

                b[row] -= factor * b[column];
            }
        }

        var x = new double[n];
        for (var row = n - 1; row >= 0; row--)
        {
            var sum = b[row];
            for (var k = row + 1; k < n; k++)
            {
                sum -= a[row][k] * x[k];
            }

            x[row] = sum / a[row][row];
        }

        return x;
    }
}
