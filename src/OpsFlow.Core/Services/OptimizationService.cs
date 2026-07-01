using OpsFlow.Core.Models;
using OpsFlow.Core.Services.Optimization;

namespace OpsFlow.Core.Services;

public enum OptimizationObjective { MaximizeROI, MinimizeCPA, MaximizeReach }

public class OptimizationConstraint
{
    public string CampaignId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // BudgetCap, MinSpend, MaxSpend, ChannelMix, ROASThreshold
    public decimal Value { get; set; }
    public string Channel { get; set; } = string.Empty;
}

public class AllocationResult
{
    public Dictionary<string, decimal> Allocations { get; set; } = new();
    public decimal TotalSpend { get; set; }
    public decimal ExpectedROI { get; set; }
    public decimal ExpectedConversions { get; set; }
    public decimal ExpectedRevenue { get; set; }
    public bool Feasible { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class OptimizationService
{
    private readonly SimplexSolver _solver = new();

    public AllocationResult Allocate(List<Campaign> campaigns, decimal totalBudget, List<OptimizationConstraint> constraints, OptimizationObjective objective = OptimizationObjective.MaximizeROI)
    {
        if (campaigns.Count == 0)
            return new AllocationResult { Feasible = false, Message = "No campaigns provided" };

        // Build the linear programming model
        int n = campaigns.Count;
        var c = new double[n]; // objective coefficients
        var lowerBounds = new double[n];
        var upperBounds = new double[n];

        for (int i = 0; i < n; i++)
        {
            var cmp = campaigns[i];

            switch (objective)
            {
                case OptimizationObjective.MaximizeROI:
                    c[i] = (double)(cmp.KPIs?.Revenue ?? 0) / Math.Max((double)cmp.Budget, 1);
                    break;
                case OptimizationObjective.MinimizeCPA:
                    c[i] = (double)(cmp.KPIs?.Conversions ?? 0) > 0
                        ? -1.0 / (double)(cmp.KPIs?.Conversions ?? 1)
                        : -1.0 / (double)(cmp.Budget > 0 ? cmp.Budget : 1);
                    break;
                case OptimizationObjective.MaximizeReach:
                    c[i] = (double)(cmp.KPIs?.Impressions ?? 0);
                    break;
            }

            lowerBounds[i] = 0;
            upperBounds[i] = (double)cmp.Budget;
        }

        // Apply custom constraints
        foreach (var constraint in constraints)
        {
            var idx = campaigns.FindIndex(c => c.Id == constraint.CampaignId);
            if (idx < 0) continue;

            switch (constraint.Type)
            {
                case "BudgetCap":
                    upperBounds[idx] = Math.Min(upperBounds[idx], (double)constraint.Value);
                    break;
                case "MinSpend":
                    lowerBounds[idx] = Math.Max(lowerBounds[idx], (double)constraint.Value);
                    break;
                case "MaxSpend":
                    upperBounds[idx] = Math.Min(upperBounds[idx], (double)constraint.Value);
                    break;
            }
        }

        // Budget constraint
        var A = new double[1][];
        A[0] = Enumerable.Repeat(1.0, n).ToArray();
        var b = new double[] { (double)totalBudget };

        var result = _solver.Solve(c, A, b, lowerBounds, upperBounds);

        var allocation = new AllocationResult
        {
            Feasible = result.Feasible,
            Message = result.Feasible ? "Optimal allocation found" : "No feasible solution",
            TotalSpend = (decimal)result.Solution.Sum()
        };

        for (int i = 0; i < n && i < result.Solution.Length; i++)
        {
            allocation.Allocations[campaigns[i].Id] = (decimal)result.Solution[i];
        }

        // Calculate expected outcomes
        allocation.ExpectedRevenue = 0;
        allocation.ExpectedConversions = 0;
        foreach (var (campId, alloc) in allocation.Allocations)
        {
            var camp = campaigns.FirstOrDefault(c => c.Id == campId);
            if (camp == null) continue;
            var ratio = camp.Budget > 0 ? (double)(alloc / camp.Budget) : 0;
            allocation.ExpectedRevenue += (decimal)((double)(camp.KPIs?.Revenue ?? 0) * ratio);
            allocation.ExpectedConversions += (decimal)((double)(camp.KPIs?.Conversions ?? 0) * ratio);
        }

        if (allocation.TotalSpend > 0)
            allocation.ExpectedROI = allocation.ExpectedRevenue / allocation.TotalSpend;

        return allocation;
    }
}
