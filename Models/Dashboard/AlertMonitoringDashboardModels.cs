namespace ITQS.SupportOperationsCenter.Models.Dashboard;

public sealed class AlertMonitoringDashboardModel
{
    public AlertMonitoringKpiModel Kpis { get; set; } = new();
    public List<AlertMonitoringSourceDistributionModel> SourceDistribution { get; set; } = new();
    public List<AlertMonitoringSeverityDistributionModel> SeverityDistribution { get; set; } = new();
    public List<AlertMonitoringTrendModel> Trends { get; set; } = new();
    public List<AlertMonitoringTopClientModel> TopClients { get; set; } = new();
    public List<AlertMonitoringTopAlertModel> TopAlerts { get; set; } = new();
    public List<AlertMonitoringRecentActivityModel> RecentActivity { get; set; } = new();
}

public sealed class AlertMonitoringKpiModel
{
    public int AffectedClients { get; set; }
    public int AffectedResources { get; set; }
    public int CriticalAlerts { get; set; }
    public int EngineersWithLoad { get; set; }
    public int ResolvedToday { get; set; }
    public decimal AverageResolutionHours { get; set; }
}

public sealed class AlertMonitoringSourceDistributionModel
{
    public string SourceType { get; set; } = string.Empty;
    public int TotalAlerts { get; set; }
    public decimal Percentage { get; set; }
}

public sealed class AlertMonitoringSeverityDistributionModel
{
    public string SeverityGroup { get; set; } = string.Empty;
    public int TotalAlerts { get; set; }
    public decimal Percentage { get; set; }
}

public sealed class AlertMonitoringTrendModel
{
    public DateTime TrendDate { get; set; }
    public int NewAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int ClosedAlerts { get; set; }
}

public sealed class AlertMonitoringTopClientModel
{
    public string ClientName { get; set; } = string.Empty;
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
}

public sealed class AlertMonitoringTopAlertModel
{
    public string AlertName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public int TotalAlerts { get; set; }
}

public sealed class AlertMonitoringRecentActivityModel
{
    public DateTime? ActivityDate { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
