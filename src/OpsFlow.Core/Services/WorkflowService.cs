using OpsFlow.Core.Models;
using OpsFlow.Core.Stores;

namespace OpsFlow.Core.Services;

public class WorkflowEngine
{
    private readonly IStore<ApprovalRequest> _requestStore;
    private readonly IStore<WorkflowDefinition> _definitionStore;

    public WorkflowEngine(IStore<ApprovalRequest> requestStore, IStore<WorkflowDefinition> definitionStore)
    {
        _requestStore = requestStore;
        _definitionStore = definitionStore;
    }

    public async Task<ApprovalRequest> CreateRequest(RequestType type, string requester, decimal amount, string campaignId)
    {
        var request = new ApprovalRequest
        {
            RequestType = type,
            Requester = requester,
            Amount = amount,
            CampaignId = campaignId,
            Status = ApprovalStatus.Pending
        };
        await _requestStore.AddAsync(request);
        return request;
    }

    public async Task<ApprovalRequest> SubmitForApproval(string requestId, string workflowDefinitionId)
    {
        var request = await _requestStore.GetByIdAsync(requestId);
        if (request == null) throw new ArgumentException("Request not found");

        var workflow = await _definitionStore.GetByIdAsync(workflowDefinitionId);
        if (workflow == null)
        {
            // Create default workflow levels based on amount
            request.Steps = CreateDefaultSteps(request.Amount);
        }
        else
        {
            request.Steps = workflow.Levels.Select(l => new ApprovalStep
            {
                ApproverId = "",
                RequiredRole = l.Role,
                MinAmount = l.MinAmount,
                MaxAmount = l.MaxAmount,
                Order = l.Level
            }).ToList();
        }

        request.CurrentStep = 0;
        request.UpdatedAt = DateTime.UtcNow;
        await _requestStore.UpdateAsync(request);
        return request;
    }

    private List<ApprovalStep> CreateDefaultSteps(decimal amount)
    {
        var steps = new List<ApprovalStep>();
        if (amount <= 50000)
        {
            steps.Add(new ApprovalStep { RequiredRole = "Manager", MinAmount = 0, MaxAmount = 50000, Order = 1 });
        }
        else if (amount <= 250000)
        {
            steps.Add(new ApprovalStep { RequiredRole = "Manager", MinAmount = 0, MaxAmount = 50000, Order = 1 });
            steps.Add(new ApprovalStep { RequiredRole = "Director", MinAmount = 50001, MaxAmount = 250000, Order = 2 });
        }
        else
        {
            steps.Add(new ApprovalStep { RequiredRole = "Manager", MinAmount = 0, MaxAmount = 50000, Order = 1 });
            steps.Add(new ApprovalStep { RequiredRole = "Director", MinAmount = 50001, MaxAmount = 250000, Order = 2 });
            steps.Add(new ApprovalStep { RequiredRole = "VP", MinAmount = 250001, MaxAmount = 1000000, Order = 3 });
            steps.Add(new ApprovalStep { RequiredRole = "CFO", MinAmount = 1000001, MaxAmount = decimal.MaxValue, Order = 4 });
        }
        return steps;
    }

    public async Task<ApprovalRequest> Approve(string requestId, string approverId, string comment)
    {
        var request = await _requestStore.GetByIdAsync(requestId);
        if (request == null) throw new ArgumentException("Request not found");
        if (request.Status != ApprovalStatus.Pending) throw new InvalidOperationException("Request is not pending");

        var currentStep = request.Steps[request.CurrentStep];
        currentStep.ApproverId = approverId;
        currentStep.Decision = ApprovalStatus.Approved;
        currentStep.DecidedAt = DateTime.UtcNow;

        request.Comments.Add(new ApprovalComment { Author = approverId, Text = comment ?? "Approved" });
        request.CurrentStep++;

        if (request.CurrentStep >= request.Steps.Count)
        {
            request.Status = ApprovalStatus.Approved;
        }

        request.UpdatedAt = DateTime.UtcNow;
        await _requestStore.UpdateAsync(request);
        return request;
    }

    public async Task<ApprovalRequest> Reject(string requestId, string approverId, string reason)
    {
        var request = await _requestStore.GetByIdAsync(requestId);
        if (request == null) throw new ArgumentException("Request not found");
        if (request.Status != ApprovalStatus.Pending) throw new InvalidOperationException("Request is not pending");

        request.Status = ApprovalStatus.Rejected;
        request.Comments.Add(new ApprovalComment { Author = approverId, Text = reason ?? "Rejected" });
        request.UpdatedAt = DateTime.UtcNow;
        await _requestStore.UpdateAsync(request);
        return request;
    }

    public async Task CheckEscalations()
    {
        var requests = await _requestStore.GetAllAsync();
        var now = DateTime.UtcNow;

        foreach (var request in requests.Where(r => r.Status == ApprovalStatus.Pending))
        {
            if ((now - request.CreatedAt).TotalDays >= 3 && request.EscalatedAt == null)
            {
                request.Status = ApprovalStatus.Escalated;
                request.EscalatedAt = now;
                request.UpdatedAt = now;
                await _requestStore.UpdateAsync(request);
            }
        }
    }

    public async Task<ApprovalRequest> GetRequest(string requestId)
    {
        return await _requestStore.GetByIdAsync(requestId) ?? throw new ArgumentException("Request not found");
    }

    public async Task<List<ApprovalRequest>> GetPendingRequests()
    {
        var all = await _requestStore.GetAllAsync();
        return all.Where(r => r.Status == ApprovalStatus.Pending).ToList();
    }
}
