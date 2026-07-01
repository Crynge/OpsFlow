namespace OpsFlow.Core.Models;

public enum CampaignStatus { Planning, Active, Paused, Completed, Cancelled }

public class Campaign
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Planning;
    public decimal Budget { get; set; }
    public decimal ActualSpend { get; set; }
    public Dictionary<string, decimal> ChannelMix { get; set; } = new();
    public CampaignKPI KPIs { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CampaignKPI
{
    public long Impressions { get; set; }
    public long Clicks { get; set; }
    public long Conversions { get; set; }
    public decimal Revenue { get; set; }
    public decimal ROAS => Revenue > 0 && Revenue > 0 ? Revenue / (Revenue > 0 ? 1 : 1) : 0;
    public decimal CPA => Conversions > 0 ? (Revenue > 0 ? Revenue / Conversions : 0) : 0;

    public decimal CalculateROAS(decimal spend) => spend > 0 ? Revenue / spend : 0;
    public decimal CalculateCPA(decimal spend) => Conversions > 0 ? spend / Conversions : 0;
}
