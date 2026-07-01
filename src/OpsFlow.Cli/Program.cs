using OpsFlow.Core.Models;
using OpsFlow.Core.Services;
using OpsFlow.Core.Services.Optimization;
using OpsFlow.Core.Stores;

var command = args.Length > 0 ? args[0].ToLower() : "help";
var subcommand = args.Length > 1 ? args[1].ToLower() : "";

var budgetStore = new InMemoryBudgetStore();
var vendorStore = new InMemoryVendorStore();
var campaignStore = new InMemoryCampaignStore();
var approvalStore = new InMemoryApprovalStore();
var workflowDefStore = new InMemoryWorkflowDefinitionStore();
var auditStore = new InMemoryAuditStore();

var budgetService = new BudgetService(budgetStore);
var optimizer = new OptimizationService();
var workflow = new WorkflowEngine(approvalStore, workflowDefStore);
var auditor = new ComplianceAuditor(auditStore, budgetStore);
var reporter = new ReportGenerator(budgetStore, vendorStore, campaignStore);

switch (command)
{
    case "budget":
        await HandleBudget(subcommand, args[2..]);
        break;
    case "optimize":
        await HandleOptimize(subcommand, args[2..]);
        break;
    case "report":
        await HandleReport(subcommand, args[2..]);
        break;
    case "serve":
        Console.WriteLine("Use 'dotnet run --project src/OpsFlow.Api' to start the API");
        break;
    case "help":
    default:
        PrintHelp();
        break;
}

async Task HandleBudget(string cmd, string[] tail)
{
    switch (cmd)
    {
        case "create":
            var name = tail.Length > 0 ? tail[0] : "Test Budget";
            var year = tail.Length > 1 ? int.Parse(tail[1]) : DateTime.UtcNow.Year;
            var amount = tail.Length > 2 ? decimal.Parse(tail[2]) : 100000m;
            var plan = await budgetService.CreateBudgetPlan(name, year, amount);
            Console.WriteLine($"Created budget plan: {plan.Id} - {plan.Name} ({plan.TotalBudget} {plan.Currency})");
            break;

        case "list":
            var plans = await budgetStore.GetAllAsync();
            Console.WriteLine($"{"ID",-10} {"Name",-20} {"Year",-6} {"Status",-10} {"Budget",-12}");
            foreach (var p in plans)
                Console.WriteLine($"{p.Id,-10} {p.Name,-20} {p.FiscalYear,-6} {p.Status,-10} {p.TotalBudget,-12:C}");
            break;

        case "show":
            var pid = tail.Length > 0 ? tail[0] : "";
            var showPlan = await budgetStore.GetByIdAsync(pid);
            if (showPlan == null) { Console.WriteLine("Plan not found"); return; }
            Console.WriteLine($"Plan: {showPlan.Name} ({showPlan.Id})");
            Console.WriteLine($"Year: {showPlan.FiscalYear} | Status: {showPlan.Status} | Budget: {showPlan.TotalBudget:C}");
            Console.WriteLine($"Line Items:");
            foreach (var li in showPlan.LineItems)
                Console.WriteLine($"  {li.CampaignName,-20} {li.Channel,-12} Planned: {li.PlannedAmount,-10:C} Spent: {li.SpentAmount,-10:C} Remaining: {li.Remaining,-10:C}");
            break;

        default:
            Console.WriteLine("Usage: dotnet run -- budget <create|list|show> [args]");
            break;
    }
}

async Task HandleOptimize(string cmd, string[] tail)
{
    if (cmd != "run") { Console.WriteLine("Usage: dotnet run -- optimize run"); return; }

    var campaigns = new List<Campaign>
    {
        new() { Id = "c1", Name = "Social Media", Budget = 50000, KPIs = new CampaignKPI { Impressions = 500000, Clicks = 25000, Conversions = 2000, Revenue = 80000 } },
        new() { Id = "c2", Name = "Search Ads", Budget = 75000, KPIs = new CampaignKPI { Impressions = 300000, Clicks = 40000, Conversions = 5000, Revenue = 150000 } },
        new() { Id = "c3", Name = "Display", Budget = 30000, KPIs = new CampaignKPI { Impressions = 800000, Clicks = 12000, Conversions = 800, Revenue = 25000 } },
        new() { Id = "c4", Name = "Email", Budget = 20000, KPIs = new CampaignKPI { Impressions = 100000, Clicks = 8000, Conversions = 1500, Revenue = 45000 } }
    };

    var constraints = new List<OptimizationConstraint>
    {
        new() { CampaignId = "c1", Type = "MinSpend", Value = 10000 },
        new() { CampaignId = "c3", Type = "MaxSpend", Value = 25000 }
    };

    var result = optimizer.Allocate(campaigns, 100000, constraints, OptimizationObjective.MaximizeROI);
    Console.WriteLine($"Optimization Result: {(result.Feasible ? "Feasible" : "Infeasible")}");
    Console.WriteLine($"Expected ROI: {result.ExpectedROI:P2}");
    Console.WriteLine($"Total Spend: {result.TotalSpend:C}");
    Console.WriteLine("Allocations:");
    foreach (var (cid, alloc) in result.Allocations)
    {
        var camp = campaigns.First(c => c.Id == cid);
        Console.WriteLine($"  {camp.Name,-15} {alloc,12:C}");
    }
}

async Task HandleReport(string cmd, string[] tail)
{
    if (cmd != "generate") { Console.WriteLine("Usage: dotnet run -- report generate <type> [planId]"); return; }

    var type = tail.Length > 0 ? tail[0] : "budget-vs-actual";
    var planId = tail.Length > 1 ? tail[1] : "";

    // Seed data for demo
    var demoPlan = await budgetService.CreateBudgetPlan("FY2026 Marketing", 2026, 500000);
    await budgetService.AddLineItem(demoPlan.Id, new BudgetLineItem { CampaignId = "c1", CampaignName = "Social Media", Channel = "Social", PlannedAmount = 100000, CommittedAmount = 60000, SpentAmount = 45000 });
    await budgetService.AddLineItem(demoPlan.Id, new BudgetLineItem { CampaignId = "c2", CampaignName = "Search Ads", Channel = "Search", PlannedAmount = 200000, CommittedAmount = 150000, SpentAmount = 120000 });
    await budgetService.AddLineItem(demoPlan.Id, new BudgetLineItem { CampaignId = "c3", CampaignName = "Display", Channel = "Display", PlannedAmount = 100000, CommittedAmount = 80000, SpentAmount = 75000 });
    await budgetService.AddLineItem(demoPlan.Id, new BudgetLineItem { CampaignId = "c4", CampaignName = "Email", Channel = "Email", PlannedAmount = 100000, CommittedAmount = 50000, SpentAmount = 35000 });

    planId = string.IsNullOrEmpty(planId) ? demoPlan.Id : planId;

    var json = await reporter.ExportToJson(type, planId);
    Console.WriteLine(json);
}

static void PrintHelp()
{
    Console.WriteLine("OpsFlow CLI - Marketing Operations Platform");
    Console.WriteLine("Usage:");
    Console.WriteLine("  budget create [name] [year] [amount]  Create a budget plan");
    Console.WriteLine("  budget list                           List budget plans");
    Console.WriteLine("  budget show <id>                      Show budget plan details");
    Console.WriteLine("  optimize run                          Run allocation optimization");
    Console.WriteLine("  report generate <type> [planId]       Generate a report (JSON)");
    Console.WriteLine("  serve                                 Start the API server");
}
