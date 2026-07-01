namespace OpsFlow.Core.Models;

public enum RequestType { BudgetAllocation, Reallocation, VendorPayment, CampaignChange, FreezeBudget }
public enum ApprovalStatus { Pending, Approved, Rejected, Escalated, Cancelled }

public class ApprovalRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public RequestType RequestType { get; set; }
    public string Requester { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CampaignId { get; set; } = string.Empty;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public List<ApprovalComment> Comments { get; set; } = new();
    public List<ApprovalStep> Steps { get; set; } = new();
    public int CurrentStep { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EscalatedAt { get; set; }
}

public class ApprovalComment
{
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ApprovalStep
{
    public string ApproverId { get; set; } = string.Empty;
    public string RequiredRole { get; set; } = string.Empty;
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; } = decimal.MaxValue;
    public int Order { get; set; }
    public ApprovalStatus? Decision { get; set; }
    public DateTime? DecidedAt { get; set; }
}

public class WorkflowDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public RequestType RequestType { get; set; }
    public List<WorkflowLevel> Levels { get; set; } = new();
    public int EscalationDays { get; set; } = 3;
}

public class WorkflowLevel
{
    public int Level { get; set; }
    public string Role { get; set; } = string.Empty;
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; } = decimal.MaxValue;
}
