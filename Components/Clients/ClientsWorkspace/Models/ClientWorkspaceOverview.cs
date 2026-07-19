namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

public sealed class ClientWorkspaceOverview
{
    public int TotalSubscriptions { get; set; }

    public int TotalVirtualMachines { get; set; }

    public int TotalDisks { get; set; }

    public int TotalAdvisorRecommendations { get; set; }

    public decimal AnnualAdvisorSavings { get; set; }

    public int TotalBackupVirtualMachines { get; set; }

    public int VirtualMachinesWithBackup { get; set; }

    public int VirtualMachinesWithoutBackup { get; set; }

    public decimal BackupCoveragePercentage { get; set; }

    public int OrphanResources { get; set; }

    public int ResourcesWithoutTags { get; set; }

    public decimal AverageCpuPercent { get; set; }

    public decimal AverageMemoryPercent { get; set; }

    public decimal AverageAvailabilityPercent { get; set; }

    public string OverallStatus { get; set; } = "Sin datos";

    public DateTime? LastOperationalUpdate { get; set; }
}
