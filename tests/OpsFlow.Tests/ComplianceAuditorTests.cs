using Xunit;
using OpsFlow.Core.Models;
using OpsFlow.Core.Services;
using OpsFlow.Core.Stores;

namespace OpsFlow.Tests;

public class ComplianceAuditorTests
{
    private readonly InMemoryAuditStore _auditStore;
    private readonly InMemoryBudgetStore _budgetStore;
    private readonly ComplianceAuditor _auditor;

    public ComplianceAuditorTests()
    {
        _auditStore = new InMemoryAuditStore();
        _budgetStore = new InMemoryBudgetStore();
        _auditor = new ComplianceAuditor(_auditStore, _budgetStore);
    }

    [Fact]
    public async Task RecordAudit_ShouldCreateEntry()
    {
        await _auditor.RecordAudit("user1", "CreateBudget", "BudgetPlan", "plan1", "", "100000");
        var trail = await _auditor.GetAuditTrail();
        Assert.Single(trail);
        Assert.Equal("user1", trail[0].UserId);
        Assert.Equal("CreateBudget", trail[0].Action);
    }

    [Fact]
    public async Task GetAuditTrail_WithFilter_ShouldReturnFiltered()
    {
        await _auditor.RecordAudit("user1", "Create", "BudgetPlan", "plan1", "", "100000");
        await _auditor.RecordAudit("user2", "Update", "BudgetPlan", "plan2", "50000", "75000");

        var filtered = await _auditor.GetAuditTrail(entityId: "plan1");
        Assert.Single(filtered);
        Assert.Equal("plan1", filtered[0].EntityId);
    }

    [Fact]
    public async Task CheckBudgetVariances_NoVariance_ShouldReturnEmpty()
    {
        var plan = new BudgetPlan { Id = "plan1", Name = "Test", TotalBudget = 100000 };
        plan.LineItems.Add(new BudgetLineItem { CampaignId = "c1", CampaignName = "C1", PlannedAmount = 50000, SpentAmount = 50000 });
        await _budgetStore.AddAsync(plan);

        var alerts = await _auditor.CheckBudgetVariances(10);
        Assert.Empty(alerts);
    }

    [Fact]
    public async Task CheckBudgetVariances_ExceedsThreshold_ShouldReturnAlerts()
    {
        var plan = new BudgetPlan { Id = "plan1", Name = "Test", TotalBudget = 100000 };
        plan.LineItems.Add(new BudgetLineItem { CampaignId = "c1", CampaignName = "C1", PlannedAmount = 50000, SpentAmount = 60000 });
        await _budgetStore.AddAsync(plan);

        var alerts = await _auditor.CheckBudgetVariances(10);
        Assert.NotEmpty(alerts);
        Assert.Equal("Warning", alerts[0].Severity);
    }

    [Fact]
    public async Task CheckBudgetVariances_Critical_ShouldFlag()
    {
        var plan = new BudgetPlan { Id = "plan1", Name = "Test", TotalBudget = 100000 };
        plan.LineItems.Add(new BudgetLineItem { CampaignId = "c1", CampaignName = "C1", PlannedAmount = 50000, SpentAmount = 70000 });
        await _budgetStore.AddAsync(plan);

        var alerts = await _auditor.CheckBudgetVariances(10);
        Assert.NotEmpty(alerts);
        Assert.Equal("Critical", alerts[0].Severity);
    }

    [Fact]
    public async Task GenerateSoxReport_WithNoViolations_ShouldPass()
    {
        var report = await _auditor.GenerateSoxReport();
        Assert.NotNull(report);
        Assert.Equal(0, report.SegregationViolations);
    }

    [Fact]
    public async Task GenerateSoxReport_WithViolations_ShouldFail()
    {
        // User who both creates and spends
        await _auditor.RecordAudit("user1", "CreateBudget", "BudgetPlan", "p1", "", "100000");
        await _auditor.RecordAudit("user1", "SpendBudget", "BudgetPlan", "p1", "50000", "55000");

        var report = await _auditor.GenerateSoxReport();
        Assert.False(report.Passed);
        Assert.True(report.SegregationViolations > 0);
    }

    [Fact]
    public async Task ValidateSegregationOfDuties_CreateAndSpend_ShouldBeInvalid()
    {
        await _auditor.RecordAudit("user1", "CreateBudget", "BudgetPlan", "p1", "", "100000");
        await _auditor.RecordAudit("user1", "SpendBudget", "BudgetPlan", "p1", "50000", "55000");

        var valid = await _auditor.ValidateSegregationOfDuties("user1", "Allocate");
        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateSegregationOfDuties_SeparateUsers_ShouldBeValid()
    {
        await _auditor.RecordAudit("user1", "CreateBudget", "BudgetPlan", "p1", "", "100000");
        await _auditor.RecordAudit("user2", "SpendBudget", "BudgetPlan", "p1", "50000", "55000");

        var valid = await _auditor.ValidateSegregationOfDuties("user1", "Allocate");
        Assert.True(valid);
    }
}
