namespace OpsFlow.Core.Services.Optimization;

public class SimplexResult
{
    public bool Feasible { get; set; }
    public double[] Solution { get; set; } = Array.Empty<double>();
    public double OptimalValue { get; set; }
    public int Iterations { get; set; }
}

public class SimplexSolver
{
    private double[][] _tableau = Array.Empty<double[]>();
    private int _rows;
    private int _cols;
    private int[] _basicVariables = Array.Empty<int>();

    public SimplexResult Solve(double[] c, double[][] A, double[] b, double[] lowerBounds, double[] upperBounds)
    {
        int n = c.Length;

        // Convert to standard form with slack/surplus variables
        // For <= constraints, add slack variables
        // For >= constraints, subtract surplus + add artificial
        // For = constraints, add artificial variables

        int m = A.Length;
        var standardA = new List<double[]>();
        var standardB = new List<double>();
        var standardC = new List<double>(c);
        int artificialCount = 0;

        for (int i = 0; i < m; i++)
        {
            double sum = 0;
            for (int j = 0; j < n; j++) sum += A[i][j] * upperBounds[j];

            if (b[i] >= 0)
            {
                // <= constraint
                var row = new double[n + 1];
                Array.Copy(A[i], row, n);
                row[n] = 1; // slack
                standardA.Add(row);
                standardB.Add(b[i]);
                standardC.Add(0);
            }
            else
            {
                // >= or = constraint - use artificial variable
                var row = new double[n + 1];
                Array.Copy(A[i], row, n);
                row[n] = -1; // surplus
                standardA.Add(row);
                standardB.Add(b[i]);
                standardC.Add(0);

                // Add artificial variable
                foreach (var r in standardA)
                {
                    Array.Resize(ref r, r.Length + 1);
                    r[^1] = 0;
                }
                standardA[^1][^1] = 1;
                standardB[^1] = Math.Abs(standardB[^1]);
                standardC.Add(-1000000); // Big M penalty
                artificialCount++;
            }
        }

        // Add bounds constraints
        for (int j = 0; j < n; j++)
        {
            if (lowerBounds[j] > 0)
            {
                var row = new double[n + standardC.Count - n];
                row[j] = 1;
                standardA.Add(row);
                standardB.Add(lowerBounds[j]);
            }
            if (upperBounds[j] < double.MaxValue)
            {
                var row = new double[n + standardC.Count - n];
                row[j] = 1;
                standardA.Add(row);
                standardB.Add(upperBounds[j]);
            }
        }

        _rows = standardA.Count;
        _cols = standardC.Count;

        // Build tableau
        _tableau = new double[_rows + 1][];
        for (int i = 0; i < _rows; i++)
        {
            _tableau[i] = new double[_cols + 1];
            for (int j = 0; j < Math.Min(standardA[i].Length, _cols); j++)
                _tableau[i][j] = standardA[i][j];
            _tableau[i][_cols] = standardB[i];
        }

        _tableau[_rows] = new double[_cols + 1];
        for (int j = 0; j < _cols; j++)
            _tableau[_rows][j] = -standardC[j]; // Negate for maximization
        _tableau[_rows][_cols] = 0;

        // Initialize basic variables
        _basicVariables = new int[_rows];
        int slackIdx = n;
        for (int i = 0; i < _rows; i++)
            _basicVariables[i] = slackIdx++;

        int iterations = 0;
        const int maxIterations = 1000;

        while (iterations < maxIterations)
        {
            // Find entering variable (Bland's rule)
            int entering = -1;
            for (int j = 0; j < _cols; j++)
            {
                if (_tableau[_rows][j] < 0)
                {
                    // Check for degeneracy - if all coefficients are non-negative, skip
                    bool allNonNegative = true;
                    for (int i = 0; i < _rows; i++)
                    {
                        if (_tableau[i][j] > 0) { allNonNegative = false; break; }
                    }
                    if (allNonNegative)
                    {
                        // Unbounded - zero out this column
                        for (int i = 0; i <= _rows; i++)
                            _tableau[i][j] = 0;
                        continue;
                    }
                    entering = j;
                    break;
                }
            }

            if (entering < 0) break; // Optimal

            // Find leaving variable (minimum ratio test)
            int leaving = -1;
            double minRatio = double.MaxValue;
            for (int i = 0; i < _rows; i++)
            {
                if (_tableau[i][entering] > 1e-10)
                {
                    double ratio = _tableau[i][_cols] / _tableau[i][entering];
                    if (ratio < minRatio && ratio >= -1e-10)
                    {
                        minRatio = ratio;
                        leaving = i;
                    }
                }
            }

            if (leaving < 0)
            {
                // Unbounded solution (should not happen with proper constraints)
                break;
            }

            // Pivot
            double pivot = _tableau[leaving][entering];
            for (int j = 0; j <= _cols; j++)
                _tableau[leaving][j] /= pivot;

            for (int i = 0; i <= _rows; i++)
            {
                if (i != leaving && Math.Abs(_tableau[i][entering]) > 1e-10)
                {
                    double factor = _tableau[i][entering];
                    for (int j = 0; j <= _cols; j++)
                        _tableau[i][j] -= factor * _tableau[leaving][j];
                }
            }

            _basicVariables[leaving] = entering;
            iterations++;
        }

        // Extract solution
        var solution = new double[n];
        for (int i = 0; i < n; i++)
        {
            int basicIdx = Array.IndexOf(_basicVariables, i);
            if (basicIdx >= 0)
                solution[i] = Math.Max(0, _tableau[basicIdx][_cols]);
            else
                solution[i] = 0;
        }

        return new SimplexResult
        {
            Feasible = iterations < maxIterations,
            Solution = solution,
            OptimalValue = _tableau[_rows][_cols],
            Iterations = iterations
        };
    }
}
