namespace ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models;

public sealed class PalmRun
{
    public Guid RunId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int? DurationSeconds { get; set; }

    public int TotalCustomers { get; set; }

    public int TotalSubscriptions { get; set; }

    public int TotalOK { get; set; }

    public int TotalNOK { get; set; }

    public decimal SuccessPercent { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class PalmReportData
{
    public PalmDashboard Dashboard { get; set; } = new();

    public PalmRun? LatestRun { get; set; }

    public List<PalmResource> Results { get; set; } = [];

    public List<PalmResource> RequiresAction { get; set; } = [];

    public List<PalmRun> RunHistory { get; set; } = [];
}
