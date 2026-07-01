using System.Collections.Concurrent;
using OpsFlow.Core.Models;
using OpsFlow.Core.Services;

namespace OpsFlow.Core.Stores;

public class InMemoryBudgetStore : IStore<BudgetPlan>
{
    private readonly ConcurrentDictionary<string, BudgetPlan> _data = new();

    public Task AddAsync(BudgetPlan entity)
    {
        _data[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _data.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<List<BudgetPlan>> GetAllAsync() => Task.FromResult(_data.Values.ToList());

    public Task<BudgetPlan?> GetByIdAsync(string id)
    {
        _data.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(BudgetPlan entity)
    {
        _data[entity.Id] = entity;
        return Task.CompletedTask;
    }
}

public class InMemoryVendorStore : IStore<Vendor>
{
    private readonly ConcurrentDictionary<string, Vendor> _data = new();

    public Task AddAsync(Vendor entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
    public Task DeleteAsync(string id) { _data.TryRemove(id, out _); return Task.CompletedTask; }
    public Task<List<Vendor>> GetAllAsync() => Task.FromResult(_data.Values.ToList());
    public Task<Vendor?> GetByIdAsync(string id) { _data.TryGetValue(id, out var e); return Task.FromResult(e); }
    public Task UpdateAsync(Vendor entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
}

public class InMemoryInvoiceStore : IStore<Invoice>
{
    private readonly ConcurrentDictionary<string, Invoice> _data = new();

    public Task AddAsync(Invoice entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
    public Task DeleteAsync(string id) { _data.TryRemove(id, out _); return Task.CompletedTask; }
    public Task<List<Invoice>> GetAllAsync() => Task.FromResult(_data.Values.ToList());
    public Task<Invoice?> GetByIdAsync(string id) { _data.TryGetValue(id, out var e); return Task.FromResult(e); }
    public Task UpdateAsync(Invoice entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
}

public class InMemoryApprovalStore : IStore<ApprovalRequest>
{
    private readonly ConcurrentDictionary<string, ApprovalRequest> _data = new();

    public Task AddAsync(ApprovalRequest entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
    public Task DeleteAsync(string id) { _data.TryRemove(id, out _); return Task.CompletedTask; }
    public Task<List<ApprovalRequest>> GetAllAsync() => Task.FromResult(_data.Values.ToList());
    public Task<ApprovalRequest?> GetByIdAsync(string id) { _data.TryGetValue(id, out var e); return Task.FromResult(e); }
    public Task UpdateAsync(ApprovalRequest entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
}

public class InMemoryWorkflowDefinitionStore : IStore<WorkflowDefinition>
{
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _data = new();

    public Task AddAsync(WorkflowDefinition entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
    public Task DeleteAsync(string id) { _data.TryRemove(id, out _); return Task.CompletedTask; }
    public Task<List<WorkflowDefinition>> GetAllAsync() => Task.FromResult(_data.Values.ToList());
    public Task<WorkflowDefinition?> GetByIdAsync(string id) { _data.TryGetValue(id, out var e); return Task.FromResult(e); }
    public Task UpdateAsync(WorkflowDefinition entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
}

public class InMemoryAuditStore : IStore<AuditEntry>
{
    private readonly ConcurrentDictionary<string, AuditEntry> _data = new();

    public Task AddAsync(AuditEntry entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
    public Task DeleteAsync(string id) { _data.TryRemove(id, out _); return Task.CompletedTask; }
    public Task<List<AuditEntry>> GetAllAsync() => Task.FromResult(_data.Values.ToList());
    public Task<AuditEntry?> GetByIdAsync(string id) { _data.TryGetValue(id, out var e); return Task.FromResult(e); }
    public Task UpdateAsync(AuditEntry entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
}

public class InMemoryCampaignStore : IStore<Campaign>
{
    private readonly ConcurrentDictionary<string, Campaign> _data = new();

    public Task AddAsync(Campaign entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
    public Task DeleteAsync(string id) { _data.TryRemove(id, out _); return Task.CompletedTask; }
    public Task<List<Campaign>> GetAllAsync() => Task.FromResult(_data.Values.ToList());
    public Task<Campaign?> GetByIdAsync(string id) { _data.TryGetValue(id, out var e); return Task.FromResult(e); }
    public Task UpdateAsync(Campaign entity) { _data[entity.Id] = entity; return Task.CompletedTask; }
}
