using OpsFlow.Core.Models;
using OpsFlow.Core.Services;
using OpsFlow.Core.Stores;

namespace OpsFlow.Tests;

public class WorkflowEngineTests
{
    private readonly WorkflowEngine _engine;
    private readonly InMemoryApprovalStore _approvalStore;
    private readonly InMemoryWorkflowDefinitionStore _workflowDefStore;

    public WorkflowEngineTests()
    {
        _approvalStore = new InMemoryApprovalStore();
        _workflowDefStore = new InMemoryWorkflowDefinitionStore();
        _engine = new WorkflowEngine(_approvalStore, _workflowDefStore);
    }

    [Fact]
    public async Task CreateRequest_ShouldCreatePendingRequest()
    {
        var request = await _engine.CreateRequest(RequestType.BudgetAllocation, "alice", 50000, "camp1");
        Assert.NotNull(request);
        Assert.Equal(ApprovalStatus.Pending, request.Status);
        Assert.Equal("alice", request.Requester);
    }

    [Fact]
    public async Task SubmitForApproval_ShouldCreateDefaultSteps()
    {
        var request = await _engine.CreateRequest(RequestType.BudgetAllocation, "alice", 25000, "camp1");
        var submitted = await _engine.SubmitForApproval(request.Id, "");
        Assert.NotEmpty(submitted.Steps);
        Assert.Equal(0, submitted.CurrentStep);
    }

    [Fact]
    public async Task Approve_FinalStep_ShouldApproveRequest()
    {
        var request = await _engine.CreateRequest(RequestType.BudgetAllocation, "alice", 25000, "camp1");
        await _engine.SubmitForApproval(request.Id, "");

        // Approve all steps
        while (request.CurrentStep < request.Steps.Count)
        {
            await _engine.Approve(request.Id, "bob", "Approved");
        }

        Assert.Equal(ApprovalStatus.Approved, request.Status);
    }

    [Fact]
    public async Task Reject_ShouldSetRejectedStatus()
    {
        var request = await _engine.CreateRequest(RequestType.BudgetAllocation, "alice", 10000, "camp1");
        await _engine.SubmitForApproval(request.Id, "");
        await _engine.Reject(request.Id, "bob", "Budget too high");
        Assert.Equal(ApprovalStatus.Rejected, request.Status);
    }

    [Fact]
    public async Task CheckEscalations_ShouldEscalateOldRequests()
    {
        var request = await _engine.CreateRequest(RequestType.BudgetAllocation, "alice", 10000, "camp1");
        request.CreatedAt = DateTime.UtcNow.AddDays(-5);
        await _approvalStore.UpdateAsync(request);

        await _engine.CheckEscalations();
        var updated = await _engine.GetRequest(request.Id);
        Assert.Equal(ApprovalStatus.Escalated, updated.Status);
    }

    [Fact]
    public async Task HighAmount_ShouldRequireMoreApprovalLevels()
    {
        var request = await _engine.CreateRequest(RequestType.BudgetAllocation, "alice", 500000, "camp1");
        await _engine.SubmitForApproval(request.Id, "");
        Assert.Equal(4, request.Steps.Count); // Manager, Director, VP, CFO
    }

    [Fact]
    public async Task LowAmount_ShouldRequireFewerLevels()
    {
        var request = await _engine.CreateRequest(RequestType.BudgetAllocation, "alice", 10000, "camp1");
        await _engine.SubmitForApproval(request.Id, "");
        Assert.Equal(1, request.Steps.Count); // Manager only
    }

    [Fact]
    public async Task Approve_NonPendingRequest_ShouldThrow()
    {
        var request = await _engine.CreateRequest(RequestType.BudgetAllocation, "alice", 10000, "camp1");
        await _engine.Reject(request.Id, "bob", "No");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _engine.Approve(request.Id, "charlie", "Approve"));
    }
}
