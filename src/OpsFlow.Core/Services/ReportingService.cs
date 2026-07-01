using System.Text.Json;
using OpsFlow.Core.Models;
using OpsFlow.Core.Stores;

namespace OpsFlow.Core.Services;

public class BudgetVsActualReport
{
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalCommitted { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercent { get; set; }
    public List<LineItemReport> LineItems { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class LineItemReport
{
    public string CampaignName { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public decimal Planned { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public decimal UtilizationPercent => Planned > 0 ? (Spent / Planned) * 100 : 0;
}

public class SpendByChannelReport
{
    public Dictionary<string, decimal> ChannelSpend { get; set; } = new();
    public decimal Total { get; set; }
}

public class RollingForecastReport
{
    public string PlanId { get; set; } = string.Empty;
    public decimal CurrentBurnRate { get; set; }
    public decimal ProjectedTotalSpend { get; set; }
    public decimal OverUnderBudget { get; set; }
    public int DaysRemaining { get; set; }
}

public class VendorSpendAnalysis
{
    public string VendorId { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public int InvoiceCount { get; set; }
}

public class RoiDashboardData
{
    public decimal TotalSpend { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OverallROAS { get; set; }
    public long TotalConversions { get; set; }
    public decimal AverageCPA { get; set; }
    public Dictionary<string, decimal> ChannelROAS { get; set; } = new();
}

public class ReportGenerator
{
    private readonly IStore<BudgetPlan> _budgetStore;
    private readonly IStore<Vendor> _vendorStore;
    private readonly IStore<Campaign> _campaignStore;

    public ReportGenerator(IStore<BudgetPlan> budgetStore, IStore<Vendor> vendorStore, IStore<Campaign> campaignStore)
    {
        _budgetStore = budgetStore;
        _vendorStore = vendorStore;
        _campaignStore = campaignStore;
    }

    public async Task<BudgetVsActualReport> GenerateBudgetVsActual(string planId)
    {
        var plan = await _budgetStore.GetByIdAsync(planId);
        if (plan == null) throw new ArgumentException("Plan not found");

        var totalSpent = plan.LineItems.Sum(li => li.SpentAmount);
        var totalCommitted = plan.LineItems.Sum(li => li.CommittedAmount);

        return new BudgetVsActualReport
        {
            PlanId = plan.Id,
            PlanName = plan.Name,
            TotalBudget = plan.TotalBudget,
            TotalSpent = totalSpent,
            TotalCommitted = totalCommitted,
            Variance = plan.TotalBudget - totalSpent,
            VariancePercent = plan.TotalBudget > 0 ? ((plan.TotalBudget - totalSpent) / plan.TotalBudget) * 100 : 0,
            LineItems = plan.LineItems.Select(li => new LineItemReport
            {
                CampaignName = li.CampaignName,
                Channel = li.Channel,
                Planned = li.PlannedAmount,
                Spent = li.SpentAmount,
                Remaining = li.Remaining
            }).ToList()
        };
    }

    public async Task<SpendByChannelReport> GenerateSpendByChannel()
    {
        var plans = await _budgetStore.GetAllAsync();
        var channelSpend = new Dictionary<string, decimal>();

        foreach (var plan in plans)
        {
            foreach (var item in plan.LineItems)
            {
                if (!channelSpend.ContainsKey(item.Channel))
                    channelSpend[item.Channel] = 0;
                channelSpend[item.Channel] += item.SpentAmount;
            }
        }

        return new SpendByChannelReport
        {
            ChannelSpend = channelSpend,
            Total = channelSpend.Values.Sum()
        };
    }

    public async Task<RollingForecastReport> GenerateRollingForecast(string planId)
    {
        var plan = await _budgetStore.GetByIdAsync(planId);
        if (plan == null) throw new ArgumentException("Plan not found");

        var totalSpent = plan.LineItems.Sum(li => li.SpentAmount);
        var daysSinceStart = (DateTime.UtcNow - plan.CreatedAt).Days;
        if (daysSinceStart < 1) daysSinceStart = 1;

        var burnRate = totalSpent / daysSinceStart;
        var remaining = 365 - daysSinceStart;
        var projectedTotal = totalSpent + (burnRate * remaining);

        return new RollingForecastReport
        {
            PlanId = plan.Id,
            CurrentBurnRate = burnRate,
            ProjectedTotalSpend = projectedTotal,
            OverUnderBudget = plan.TotalBudget - projectedTotal,
            DaysRemaining = Math.Max(0, remaining)
        };
    }

    public async Task<List<VendorSpendAnalysis>> GenerateVendorAnalysis()
    {
        var vendors = await _vendorStore.GetAllAsync();
        return vendors.Select(v => new VendorSpendAnalysis
        {
            VendorId = v.Id,
            VendorName = v.Name,
            TotalInvoiced = v.Invoices.Sum(i => i.Amount),
            TotalPaid = v.Invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount),
            Outstanding = v.Invoices.Where(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue).Sum(i => i.Amount),
            InvoiceCount = v.Invoices.Count
        }).ToList();
    }

    public async Task<RoiDashboardData> GenerateRoiDashboard()
    {
        var campaigns = await _campaignStore.GetAllAsync();
        var totalSpend = campaigns.Sum(c => c.ActualSpend);
        var totalRevenue = campaigns.Sum(c => c.KPIs?.Revenue ?? 0);
        var totalConversions = campaigns.Sum(c => c.KPIs?.Conversions ?? 0);

        var channelROAS = new Dictionary<string, decimal>();
        foreach (var c in campaigns)
        {
            foreach (var (channel, spend) in c.ChannelMix)
            {
                if (!channelROAS.ContainsKey(channel)) channelROAS[channel] = 0;
                var ratio = c.Budget > 0 ? spend / c.Budget : 0;
                channelROAS[channel] += (c.KPIs?.Revenue ?? 0) * ratio;
            }
        }

        return new RoiDashboardData
        {
            TotalSpend = totalSpend,
            TotalRevenue = totalRevenue,
            OverallROAS = totalSpend > 0 ? totalRevenue / totalSpend : 0,
            TotalConversions = totalConversions,
            AverageCPA = totalConversions > 0 ? totalSpend / totalConversions : 0,
            ChannelROAS = channelROAS
        };
    }

    public async Task<string> ExportToCsv(string reportType, string planId = "")
    {
        var lines = new List<string>();
        switch (reportType.ToLower())
        {
            case "budget-vs-actual":
                var report = await GenerateBudgetVsActual(planId);
                lines.Add("Campaign,Channel,Planned,Spent,Remaining,Utilization%");
                foreach (var li in report.LineItems)
                    lines.Add($"{li.CampaignName},{li.Channel},{li.Planned},{li.Spent},{li.Remaining},{li.UtilizationPercent:F2}");
                break;
            case "vendor-analysis":
                var vendors = await GenerateVendorAnalysis();
                lines.Add("Vendor,Invoiced,Paid,Outstanding,Invoices");
                foreach (var v in vendors)
                    lines.Add($"{v.VendorName},{v.TotalInvoiced},{v.TotalPaid},{v.Outstanding},{v.InvoiceCount}");
                break;
        }
        return string.Join(Environment.NewLine, lines);
    }

    public async Task<string> ExportToJson(string reportType, string planId = "")
    {
        return reportType.ToLower() switch
        {
            "budget-vs-actual" => JsonSerializer.Serialize(await GenerateBudgetVsActual(planId), new JsonSerializerOptions { WriteIndented = true }),
            "spend-by-channel" => JsonSerializer.Serialize(await GenerateSpendByChannel(), new JsonSerializerOptions { WriteIndented = true }),
            "rolling-forecast" => JsonSerializer.Serialize(await GenerateRollingForecast(planId), new JsonSerializerOptions { WriteIndented = true }),
            "vendor-analysis" => JsonSerializer.Serialize(await GenerateVendorAnalysis(), new JsonSerializerOptions { WriteIndented = true }),
            "roi-dashboard" => JsonSerializer.Serialize(await GenerateRoiDashboard(), new JsonSerializerOptions { WriteIndented = true }),
            _ => throw new ArgumentException($"Unknown report type: {reportType}")
        };
    }
}
