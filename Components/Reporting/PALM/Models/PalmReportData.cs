namespace ITQS.SupportOperationsCenter.Components.Reporting.PALM.Models;

public sealed class PalmReportData
{
    public PalmDashboard Dashboard { get; set; } = new();

    public PalmRun? LatestRun { get; set; }

    public IReadOnlyList<PalmResource> Results { get; set; }
        = Array.Empty<PalmResource>();

    public IReadOnlyList<PalmResource> RequiresAction { get; set; }
        = Array.Empty<PalmResource>();

    public IReadOnlyList<PalmRun> RunHistory { get; set; }
        = Array.Empty<PalmRun>();
}
