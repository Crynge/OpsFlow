using Xunit;
using OpsFlow.Core.Services.Optimization;

namespace OpsFlow.Tests;

public class SimplexSolverTests
{
    private readonly SimplexSolver _solver = new();

    [Fact]
    public void Solve_SimpleMaximize_ShouldFindOptimal()
    {
        // Maximize z = 3x + 2y subject to:
        // x + y <= 4
        // 2x + y <= 5
        // x, y >= 0
        // Optimal: x=1, y=3, z=9
        double[] c = { 3, 2 };
        double[][] A = {
            new double[] { 1, 1 },
            new double[] { 2, 1 }
        };
        double[] b = { 4, 5 };
        double[] lower = { 0, 0 };
        double[] upper = { double.MaxValue, double.MaxValue };

        var result = _solver.Solve(c, A, b, lower, upper);

        Assert.True(result.Feasible);
        Assert.Equal(2, result.Solution.Length);
        Assert.Equal(1, result.Solution[0], 1);
        Assert.Equal(3, result.Solution[1], 1);
        Assert.Equal(9, result.OptimalValue, 1);
    }

    [Fact]
    public void Solve_WithBounds_ShouldRespectConstraints()
    {
        // Maximize z = 2x + 3y subject to:
        // x + y <= 10
        // x <= 4
        // y <= 6
        // x, y >= 0
        double[] c = { 2, 3 };
        double[][] A = { new double[] { 1, 1 } };
        double[] b = { 10 };
        double[] lower = { 0, 0 };
        double[] upper = { 4, 6 };

        var result = _solver.Solve(c, A, b, lower, upper);

        Assert.True(result.Feasible);
        Assert.Equal(4, result.Solution[0], 1);
        Assert.Equal(6, result.Solution[1], 1);
    }

    [Fact]
    public void Solve_SingleVariable_ShouldFindSolution()
    {
        double[] c = { 5 };
        double[][] A = { new double[] { 1 } };
        double[] b = { 100 };
        double[] lower = { 0 };
        double[] upper = { double.MaxValue };

        var result = _solver.Solve(c, A, b, lower, upper);

        Assert.True(result.Feasible);
        Assert.Equal(100, result.Solution[0], 1);
        Assert.Equal(500, result.OptimalValue, 1);
    }

    [Fact]
    public void Solve_ZeroBudget_ShouldReturnZeros()
    {
        double[] c = { 3, 2 };
        double[][] A = { new double[] { 1, 1 } };
        double[] b = { 0 };
        double[] lower = { 0, 0 };
        double[] upper = { double.MaxValue, double.MaxValue };

        var result = _solver.Solve(c, A, b, lower, upper);

        Assert.True(result.Feasible);
        Assert.Equal(0, result.Solution[0], 1);
        Assert.Equal(0, result.Solution[1], 1);
    }
}
