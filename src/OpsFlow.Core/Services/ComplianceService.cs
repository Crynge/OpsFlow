using OpsFlow.Core.Models;
using OpsFlow.Core.Stores;

namespace OpsFlow.Core.Services;

public class AuditEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class BudgetVarianceAlert
{
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string CampaignId { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal VariancePercent { get; set; }
    public string Severity { get; set; } = "Info";
}

public class SoxComplianceReport
{
    public string ReportId { get; set; } = Guid.NewGuid().ToString();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalAuditEntries { get; set; }
    public int SegregationViolations { get; set; }
    public int BudgetVarianceFlags { get; set; }
    public List<string> Findings { get; set; } = new();
    public bool Passed { get; set; }
}

public class ComplianceAuditor
{
    private readonly IStore<AuditEntry> _auditStore;
    private readonly IStore<BudgetPlan> _budgetStore;

    public ComplianceAuditor(IStore<AuditEntry> auditStore, IStore<BudgetPlan> budgetStore)
    {
        _auditStore = auditStore;
        _budgetStore = budgetStore;
    }

    public async Task RecordAudit(string userId, string action, string entityType, string entityId, string oldValue, string newValue)
    {
        var entry = new AuditEntry
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue
        };
        await _auditStore.AddAsync(entry);
    }

    public async Task<List<AuditEntry>> GetAuditTrail(string? entityId = null, DateTime? from = null, DateTime? to = null)
    {
        var entries = await _auditStore.GetAllAsync();

        if (!string.IsNullOrEmpty(entityId))
            entries = entries.Where(e => e.EntityId == entityId).ToList();
        if (from.HasValue)
            entries = entries.Where(e => e.Timestamp >= from.Value).ToList();
        if (to.HasValue)
            entries = entries.Where(e => e.Timestamp <= to.Value).ToList();

        return entries.OrderByDescending(e => e.Timestamp).ToList();
    }

    public async Task<List<BudgetVarianceAlert>> CheckBudgetVariances(decimal thresholdPercent = 10)
    {
        var alerts = new List<BudgetVarianceAlert>();
        var plans = await _budgetStore.GetAllAsync();

        foreach (var plan in plans)
        {
            foreach (var item in plan.LineItems)
            {
                if (item.PlannedAmount <= 0) continue;

                var variance = ((item.SpentAmount - item.PlannedAmount) / item.PlannedAmount) * 100;
                if (Math.Abs((double)variance) > (double)thresholdPercent)
                {
                    alerts.Add(new BudgetVarianceAlert
                    {
                        PlanId = plan.Id,
                        PlanName = plan.Name,
                        CampaignId = item.CampaignId,
                        CampaignName = item.CampaignName,
                        PlannedAmount = item.PlannedAmount,
                        ActualAmount = item.SpentAmount,
                        VariancePercent = variance,
                        Severity = Math.Abs((double)variance) > 20 ? "Critical" : "Warning"
                    });
                }
            }
        }

        return alerts;
    }

    public async Task<SoxComplianceReport> GenerateSoxReport()
    {
        var entries = await _auditStore.GetAllAsync();
        var variances = await CheckBudgetVariances();
        var findings = new List<string>();

        // Check segregation of duties
        var userActions = entries.GroupBy(e => e.UserId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Action).Distinct().ToList());

        var segregationViolations = 0;
        foreach (var (userId, actions) in userActions)
        {
            var creating = actions.Any(a => a.Contains("Create") || a.Contains("Approve"));
            var spending = actions.Any(a => a.Contains("Spend") || a.Contains("Pay"));
            if (creating && spending)
            {
                segregationViolations++;
                findings.Add($"User {userId} has both creation and spending authority (SoD violation)");
            }
        }

        foreach (var v in variances.Where(v => v.Severity == "Critical"))
        {
            findings.Add($"Critical budget variance: {v.CampaignName} - {v.VariancePercent:F1}% deviation");
        }

        if (!entries.Any())
            findings.Add("No audit entries found for review period");

        return new SoxComplianceReport
        {
            TotalAuditEntries = entries.Count,
            SegregationViolations = segregationViolations,
            BudgetVarianceFlags = variances.Count(v => v.Severity == "Critical"),
            Findings = findings,
            Passed = segregationViolations == 0 && variances.Count(v => v.Severity == "Critical") == 0
        };
    }

    public async Task<bool> ValidateSegregationOfDuties(string userId, string action)
    {
        var entries = await _auditStore.GetAllAsync();
        var userActions = entries.Where(e => e.UserId == userId).Select(e => e.Action).Distinct().ToList();

        var isCreatorAction = action.Contains("Create") || action.Contains("Approve") || action.Contains("Allocate");
        var hasSpendAuthority = userActions.Any(a => a.Contains("Spend") || a.Contains("Pay") || a.Contains("Reallocate"));

        return !(isCreatorAction && hasSpendAuthority);
    }
}
