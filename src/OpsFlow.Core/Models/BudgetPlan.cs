namespace OpsFlow.Core.Models;

public enum BudgetStatus { Draft, Active, Frozen, Closed }

public class BudgetPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;
    public decimal TotalBudget { get; set; }
    public string Currency { get; set; } = "USD";
    public List<BudgetLineItem> LineItems { get; set; } = new();
    public List<BudgetVersion> Versions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class BudgetLineItem
{
    public string CampaignId { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public decimal PlannedAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal Remaining => PlannedAmount - SpentAmount;
    public decimal Available => PlannedAmount - CommittedAmount;
    public decimal CommitRatio => PlannedAmount > 0 ? CommittedAmount / PlannedAmount : 0;
    public string Status { get; set; } = "Active";
}

public class BudgetVersion
{
    public int VersionNumber { get; set; }
    public string Changes { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
}
