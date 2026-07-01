using OpsFlow.Core.Models;
using OpsFlow.Core.Services;
using OpsFlow.Core.Stores;

namespace OpsFlow.Tests;

public class BudgetServiceTests
{
    private readonly BudgetService _service;
    private readonly InMemoryBudgetStore _store;

    public BudgetServiceTests()
    {
        _store = new InMemoryBudgetStore();
        _service = new BudgetService(_store);
    }

    [Fact]
    public async Task CreateBudgetPlan_ShouldCreatePlan()
    {
        var plan = await _service.CreateBudgetPlan("Test", 2026, 100000);
        Assert.NotNull(plan);
        Assert.Equal("Test", plan.Name);
        Assert.Equal(2026, plan.FiscalYear);
        Assert.Equal(100000, plan.TotalBudget);
        Assert.Equal(BudgetStatus.Draft, plan.Status);
    }

    [Fact]
    public async Task AddLineItem_ShouldAddToPlan()
    {
        var plan = await _service.CreateBudgetPlan("Test", 2026, 100000);
        var item = new BudgetLineItem { CampaignId = "c1", CampaignName = "Camp1", Channel = "Social", PlannedAmount = 50000 };
        var added = await _service.AddLineItem(plan.Id, item);
        Assert.Equal("c1", added.CampaignId);
        Assert.Single(plan.LineItems);
    }

    [Fact]
    public async Task AddLineItem_ExceedsBudget_ShouldThrow()
    {
        var plan = await _service.CreateBudgetPlan("Test", 2026, 10000);
        var item = new BudgetLineItem { CampaignId = "c1", CampaignName = "Camp1", Channel = "Social", PlannedAmount = 50000 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddLineItem(plan.Id, item));
    }

    [Fact]
    public async Task UpdateSpend_ShouldUpdateAmounts()
    {
        var plan = await _service.CreateBudgetPlan("Test", 2026, 100000);
        await _service.AddLineItem(plan.Id, new BudgetLineItem { CampaignId = "c1", CampaignName = "Camp1", Channel = "Social", PlannedAmount = 50000 });
        await _service.UpdateSpend(plan.Id, "c1", 25000, 30000);
        var item = plan.LineItems.First();
        Assert.Equal(25000, item.SpentAmount);
        Assert.Equal(30000, item.CommittedAmount);
    }

    [Fact]
    public async Task GetRemainingBudget_ShouldReturnCorrectAmount()
    {
        var plan = await _service.CreateBudgetPlan("Test", 2026, 100000);
        await _service.AddLineItem(plan.Id, new BudgetLineItem { CampaignId = "c1", CampaignName = "Camp1", Channel = "Social", PlannedAmount = 50000 });
        await _service.UpdateSpend(plan.Id, "c1", 30000, 35000);
        var remaining = await _service.GetRemainingBudget(plan.Id);
        Assert.Equal(70000, remaining); // 100000 - 30000
    }

    [Fact]
    public async Task Reallocate_WithoutApprovalThreshold_ShouldSucceed()
    {
        var plan = await _service.CreateBudgetPlan("Test", 2026, 100000);
        plan.Status = BudgetStatus.Active;
        await _store.UpdateAsync(plan);
        await _service.AddLineItem(plan.Id, new BudgetLineItem { CampaignId = "c1", CampaignName = "Camp1", Channel = "Social", PlannedAmount = 60000 });
        await _service.AddLineItem(plan.Id, new BudgetLineItem { CampaignId = "c2", CampaignName = "Camp2", Channel = "Search", PlannedAmount = 40000 });
        await _service.Reallocate(plan.Id, "c1", "c2", 5000, true);
        Assert.Equal(55000, plan.LineItems[0].PlannedAmount);
        Assert.Equal(45000, plan.LineItems[1].PlannedAmount);
    }

    [Fact]
    public async Task Reallocate_OverThresholdWithoutApproval_ShouldThrow()
    {
        var plan = await _service.CreateBudgetPlan("Test", 2026, 100000);
        plan.Status = BudgetStatus.Active;
        await _store.UpdateAsync(plan);
        await _service.AddLineItem(plan.Id, new BudgetLineItem { CampaignId = "c1", CampaignName = "Camp1", Channel = "Social", PlannedAmount = 60000 });
        await _service.AddLineItem(plan.Id, new BudgetLineItem { CampaignId = "c2", CampaignName = "Camp2", Channel = "Search", PlannedAmount = 40000 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.Reallocate(plan.Id, "c1", "c2", 15000, false));
    }

    [Fact]
    public async Task FreezeBudget_ShouldSetFrozenStatus()
    {
        var plan = await _service.CreateBudgetPlan("Test", 2026, 100000);
        plan.Status = BudgetStatus.Active;
        await _store.UpdateAsync(plan);
        await _service.FreezeBudget(plan.Id);
        Assert.Equal(BudgetStatus.Frozen, plan.Status);
    }

    [Fact]
    public async Task CloseBudget_ShouldSetClosedStatus()
    {
        var plan = await _service.CreateBudgetPlan("Test", 2026, 100000);
        plan.Status = BudgetStatus.Active;
        await _store.UpdateAsync(plan);
        await _service.FreezeBudget(plan.Id);
        await _service.CloseBudget(plan.Id);
        Assert.Equal(BudgetStatus.Closed, plan.Status);
    }

    [Fact]
    public async Task GetForecast_ShouldReturnProjection()
    {
        var plan = await _service.CreateBudgetPlan("Test", 2026, 100000);
        await _service.AddLineItem(plan.Id, new BudgetLineItem { CampaignId = "c1", CampaignName = "Camp1", Channel = "Social", PlannedAmount = 50000 });
        await _service.UpdateSpend(plan.Id, "c1", 25000, 25000);
        var forecast = await _service.GetForecast(plan.Id);
        Assert.NotNull(forecast);
        Assert.Equal(100000, forecast.TotalBudget);
        Assert.Equal(25000, forecast.TotalSpent);
    }
}
