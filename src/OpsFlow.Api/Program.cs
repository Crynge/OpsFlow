using OpsFlow.Core.Models;
using OpsFlow.Core.Services;
using OpsFlow.Core.Services.Optimization;
using OpsFlow.Core.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IStore<BudgetPlan>, InMemoryBudgetStore>();
builder.Services.AddSingleton<IStore<Vendor>, InMemoryVendorStore>();
builder.Services.AddSingleton<IStore<Invoice>, InMemoryInvoiceStore>();
builder.Services.AddSingleton<IStore<ApprovalRequest>, InMemoryApprovalStore>();
builder.Services.AddSingleton<IStore<WorkflowDefinition>, InMemoryWorkflowDefinitionStore>();
builder.Services.AddSingleton<IStore<AuditEntry>, InMemoryAuditStore>();
builder.Services.AddSingleton<IStore<Campaign>, InMemoryCampaignStore>();
builder.Services.AddSingleton<BudgetService>();
builder.Services.AddSingleton<OptimizationService>();
builder.Services.AddSingleton<WorkflowEngine>();
builder.Services.AddSingleton<ComplianceAuditor>();
builder.Services.AddSingleton<ReportGenerator>();

var app = builder.Build();

// Budget endpoints
app.MapGet("/api/budgets", async (BudgetService bs) =>
{
    var store = app.Services.GetRequiredService<IStore<BudgetPlan>>();
    return Results.Ok(await store.GetAllAsync());
});

app.MapPost("/api/budgets", async (BudgetPlan plan, BudgetService bs) =>
{
    var created = await bs.CreateBudgetPlan(plan.Name, plan.FiscalYear, plan.TotalBudget, plan.Currency);
    return Results.Created($"/api/budgets/{created.Id}", created);
});

app.MapGet("/api/budgets/{id}", async (string id, IStore<BudgetPlan> store) =>
{
    var plan = await store.GetByIdAsync(id);
    return plan is not null ? Results.Ok(plan) : Results.NotFound();
});

app.MapPut("/api/budgets/{id}/reallocate", async (string id, ReallocateRequest req, BudgetService bs) =>
{
    try
    {
        await bs.Reallocate(id, req.FromCampaignId, req.ToCampaignId, req.Amount, req.HasApproval);
        return Results.Ok(new { message = "Reallocation successful" });
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPost("/api/budgets/{id}/freeze", async (string id, BudgetService bs) =>
{
    try { await bs.FreezeBudget(id); return Results.Ok(new { message = "Budget frozen" }); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapGet("/api/budgets/{id}/forecast", async (string id, BudgetService bs) =>
{
    try { return Results.Ok(await bs.GetForecast(id)); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// Optimization endpoint
app.MapPost("/api/optimize", async (OptimizeRequest req, OptimizationService os) =>
{
    var camps = req.Campaigns.Select(c => new Campaign
    {
        Id = c.Id,
        Name = c.Name,
        Budget = c.Budget,
        KPIs = new CampaignKPI
        {
            Impressions = c.Impressions,
            Clicks = c.Clicks,
            Conversions = c.Conversions,
            Revenue = c.Revenue
        }
    }).ToList();

    var constraints = req.Constraints?.Select(c => new OptimizationConstraint
    {
        CampaignId = c.CampaignId,
        Type = c.Type,
        Value = c.Value
    }).ToList() ?? new();

    var result = os.Allocate(camps, req.TotalBudget, constraints, req.Objective);
    return Results.Ok(result);
});

// Workflow endpoints
app.MapPost("/api/workflows/approve", async (ApproveRequest req, WorkflowEngine we) =>
{
    try
    {
        var result = await we.Approve(req.RequestId, req.ApproverId, req.Comment);
        return Results.Ok(result);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPost("/api/workflows/reject", async (ApproveRequest req, WorkflowEngine we) =>
{
    try
    {
        var result = await we.Reject(req.RequestId, req.ApproverId, req.Comment);
        return Results.Ok(result);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// Compliance endpoints
app.MapGet("/api/compliance/audit-log", async (ComplianceAuditor ca, string? entityId, DateTime? from, DateTime? to) =>
{
    return Results.Ok(await ca.GetAuditTrail(entityId, from, to));
});

app.MapGet("/api/compliance/variances", async (ComplianceAuditor ca) =>
{
    return Results.Ok(await ca.CheckBudgetVariances());
});

app.MapGet("/api/compliance/sox-report", async (ComplianceAuditor ca) =>
{
    return Results.Ok(await ca.GenerateSoxReport());
});

// Report endpoints
app.MapGet("/api/reports/budget-vs-actual", async (string planId, ReportGenerator rg) =>
{
    try { return Results.Ok(await rg.GenerateBudgetVsActual(planId)); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapGet("/api/reports/spend-by-channel", async (ReportGenerator rg) =>
{
    return Results.Ok(await rg.GenerateSpendByChannel());
});

app.MapGet("/api/reports/vendor-analysis", async (ReportGenerator rg) =>
{
    return Results.Ok(await rg.GenerateVendorAnalysis());
});

app.MapGet("/api/reports/roi-dashboard", async (ReportGenerator rg) =>
{
    return Results.Ok(await rg.GenerateRoiDashboard());
});

app.MapGet("/api/reports/export", async (string type, string? planId, ReportGenerator rg) =>
{
    try { return Results.Ok(await rg.ExportToCsv(type, planId ?? "")); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// Health
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

app.Run();

// Request DTOs
record ReallocateRequest(string FromCampaignId, string ToCampaignId, decimal Amount, bool HasApproval);
record OptimizeRequest(List<CampaignDto> Campaigns, decimal TotalBudget, List<ConstraintDto>? Constraints, OptimizationObjective Objective);
record CampaignDto(string Id, string Name, decimal Budget, long Impressions, long Clicks, long Conversions, decimal Revenue);
record ConstraintDto(string CampaignId, string Type, decimal Value);
record ApproveRequest(string RequestId, string ApproverId, string Comment);
