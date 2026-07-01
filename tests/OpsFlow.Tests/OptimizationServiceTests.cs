using Xunit;
using OpsFlow.Core.Models;
using OpsFlow.Core.Services;

namespace OpsFlow.Tests;

public class OptimizationServiceTests
{
    private readonly OptimizationService _service = new();

    private List<Campaign> GetSampleCampaigns()
    {
        return new List<Campaign>
        {
            new() { Id = "c1", Name = "Social", Budget = 50000, KPIs = new CampaignKPI { Impressions = 500000, Clicks = 25000, Conversions = 2000, Revenue = 80000 } },
            new() { Id = "c2", Name = "Search", Budget = 75000, KPIs = new CampaignKPI { Impressions = 300000, Clicks = 40000, Conversions = 5000, Revenue = 150000 } },
            new() { Id = "c3", Name = "Display", Budget = 30000, KPIs = new CampaignKPI { Impressions = 800000, Clicks = 12000, Conversions = 800, Revenue = 25000 } }
        };
    }

    [Fact]
    public void Allocate_MaximizeROI_ShouldReturnAllocations()
    {
        var campaigns = GetSampleCampaigns();
        var result = _service.Allocate(campaigns, 100000, new(), OptimizationObjective.MaximizeROI);

        Assert.True(result.Feasible);
        Assert.Equal(3, result.Allocations.Count);
        Assert.True(result.TotalSpend <= 100000);
    }

    [Fact]
    public void Allocate_WithConstraints_ShouldRespectMinSpend()
    {
        var campaigns = GetSampleCampaigns();
        var constraints = new List<OptimizationConstraint>
        {
            new() { CampaignId = "c1", Type = "MinSpend", Value = 20000 }
        };
        var result = _service.Allocate(campaigns, 100000, constraints, OptimizationObjective.MaximizeROI);

        Assert.True(result.Feasible);
        Assert.True(result.Allocations["c1"] >= 20000);
    }

    [Fact]
    public void Allocate_MaximizeReach_ShouldPrioritizeImpressions()
    {
        var campaigns = GetSampleCampaigns();
        var result = _service.Allocate(campaigns, 100000, new(), OptimizationObjective.MaximizeReach);

        Assert.True(result.Feasible);
        // Display has highest impressions, should get significant allocation
        Assert.True(result.Allocations["c3"] > 0);
    }

    [Fact]
    public void Allocate_WithBudgetCap_ShouldLimitCampaign()
    {
        var campaigns = GetSampleCampaigns();
        var constraints = new List<OptimizationConstraint>
        {
            new() { CampaignId = "c2", Type = "BudgetCap", Value = 10000 }
        };
        var result = _service.Allocate(campaigns, 100000, constraints, OptimizationObjective.MaximizeROI);

        Assert.True(result.Feasible);
        Assert.True(result.Allocations["c2"] <= 10000);
    }

    [Fact]
    public void Allocate_EmptyCampaigns_ShouldReturnInfeasible()
    {
        var result = _service.Allocate(new(), 100000, new(), OptimizationObjective.MaximizeROI);
        Assert.False(result.Feasible);
        Assert.Equal("No campaigns provided", result.Message);
    }
}
