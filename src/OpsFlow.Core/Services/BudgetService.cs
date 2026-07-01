using OpsFlow.Core.Models;
using OpsFlow.Core.Stores;

namespace OpsFlow.Core.Services;

public class BudgetService
{
    private readonly IStore<BudgetPlan> _budgetStore;

    public BudgetService(IStore<BudgetPlan> budgetStore)
    {
        _budgetStore = budgetStore;
    }

    public async Task<BudgetPlan> CreateBudgetPlan(string name, int fiscalYear, decimal totalBudget, string currency = "USD")
    {
        var plan = new BudgetPlan
        {
            Name = name,
            FiscalYear = fiscalYear,
            TotalBudget = totalBudget,
            Currency = currency
        };
        await _budgetStore.AddAsync(plan);
        return plan;
    }

    public async Task<BudgetLineItem> AddLineItem(string planId, BudgetLineItem item)
    {
        var plan = await _budgetStore.GetByIdAsync(planId);
        if (plan == null) throw new ArgumentException("Budget plan not found");
        if (plan.Status != BudgetStatus.Draft && plan.Status != BudgetStatus.Active)
            throw new InvalidOperationException("Can only add line items to Draft or Active plans");

        var totalPlanned = plan.LineItems.Sum(li => li.PlannedAmount) + item.PlannedAmount;
        if (totalPlanned > plan.TotalBudget)
            throw new InvalidOperationException($"Line item would exceed total budget. Planned: {totalPlanned}, Budget: {plan.TotalBudget}");

        plan.LineItems.Add(item);
        plan.UpdatedAt = DateTime.UtcNow;
        await _budgetStore.UpdateAsync(plan);
        return item;
    }

    public async Task UpdateSpend(string planId, string campaignId, decimal spentAmount, decimal committedAmount)
    {
        var plan = await _budgetStore.GetByIdAsync(planId);
        if (plan == null) throw new ArgumentException("Budget plan not found");

        var item = plan.LineItems.FirstOrDefault(li => li.CampaignId == campaignId);
        if (item == null) throw new ArgumentException("Line item not found");

        item.SpentAmount = spentAmount;
        item.CommittedAmount = committedAmount;
        plan.UpdatedAt = DateTime.UtcNow;
        await _budgetStore.UpdateAsync(plan);
    }

    public async Task<decimal> GetRemainingBudget(string planId)
    {
        var plan = await _budgetStore.GetByIdAsync(planId);
        if (plan == null) throw new ArgumentException("Budget plan not found");
        return plan.TotalBudget - plan.LineItems.Sum(li => li.SpentAmount);
    }

    public async Task<decimal> GetAvailableBudget(string planId)
    {
        var plan = await _budgetStore.GetByIdAsync(planId);
        if (plan == null) throw new ArgumentException("Budget plan not found");
        return plan.TotalBudget - plan.LineItems.Sum(li => li.CommittedAmount);
    }

    public async Task Reallocate(string planId, string fromCampaignId, string toCampaignId, decimal amount, bool hasApproval = false)
    {
        if (!hasApproval && amount > 10000)
            throw new InvalidOperationException("Reallocation over $10,000 requires approval");

        var plan = await _budgetStore.GetByIdAsync(planId);
        if (plan == null) throw new ArgumentException("Budget plan not found");
        if (plan.Status != BudgetStatus.Active)
            throw new InvalidOperationException("Can only reallocate in Active plans");

        var fromItem = plan.LineItems.FirstOrDefault(li => li.CampaignId == fromCampaignId);
        var toItem = plan.LineItems.FirstOrDefault(li => li.CampaignId == toCampaignId);
        if (fromItem == null || toItem == null) throw new ArgumentException("Campaign not found");

        var fromAvailable = fromItem.PlannedAmount - fromItem.CommittedAmount;
        if (amount > fromAvailable)
            throw new InvalidOperationException($"Insufficient available budget in source. Available: {fromAvailable}, Requested: {amount}");

        fromItem.PlannedAmount -= amount;
        toItem.PlannedAmount += amount;
        plan.UpdatedAt = DateTime.UtcNow;
        await _budgetStore.UpdateAsync(plan);
    }

    public async Task FreezeBudget(string planId)
    {
        var plan = await _budgetStore.GetByIdAsync(planId);
        if (plan == null) throw new ArgumentException("Budget plan not found");
        if (plan.Status != BudgetStatus.Active)
            throw new InvalidOperationException("Only Active plans can be frozen");

        plan.Status = BudgetStatus.Frozen;
        plan.UpdatedAt = DateTime.UtcNow;
        await _budgetStore.UpdateAsync(plan);
    }

    public async Task CloseBudget(string planId)
    {
        var plan = await _budgetStore.GetByIdAsync(planId);
        if (plan == null) throw new ArgumentException("Budget plan not found");
        if (plan.Status != BudgetStatus.Frozen && plan.Status != BudgetStatus.Active)
            throw new InvalidOperationException("Only Frozen or Active plans can be closed");

        plan.Status = BudgetStatus.Closed;
        plan.UpdatedAt = DateTime.UtcNow;
        await _budgetStore.UpdateAsync(plan);
    }

    public async Task<ForecastResult> GetForecast(string planId)
    {
        var plan = await _budgetStore.GetByIdAsync(planId);
        if (plan == null) throw new ArgumentException("Budget plan not found");

        var totalSpent = plan.LineItems.Sum(li => li.SpentAmount);
        var daysSinceStart = (DateTime.UtcNow - plan.CreatedAt).Days;
        if (daysSinceStart < 1) daysSinceStart = 1;

        var dailyBurnRate = totalSpent / daysSinceStart;
        var remaining = plan.TotalBudget - totalSpent;
        var daysRemaining = dailyBurnRate > 0 ? remaining / dailyBurnRate : 365;
        var projectedExhaustionDate = DateTime.UtcNow.AddDays((double)daysRemaining);
        var willExhaust = dailyBurnRate > 0 && daysRemaining < 365;

        return new ForecastResult
        {
            TotalBudget = plan.TotalBudget,
            TotalSpent = totalSpent,
            Remaining = remaining,
            DailyBurnRate = dailyBurnRate,
            DaysUntilExhaustion = (int)daysRemaining,
            ProjectedExhaustionDate = projectedExhaustionDate,
            WillExhaustBeforeYearEnd = willExhaust
        };
    }
}

public class ForecastResult
{
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining { get; set; }
    public decimal DailyBurnRate { get; set; }
    public int DaysUntilExhaustion { get; set; }
    public DateTime ProjectedExhaustionDate { get; set; }
    public bool WillExhaustBeforeYearEnd { get; set; }
}
