namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Infrastructure;

public sealed class VirtualMachineModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;

    public string ResourceId { get; set; } = string.Empty;
    public string VMName { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;

    public string PowerState { get; set; } = string.Empty;
    public string ProvisioningState { get; set; } = string.Empty;

    public string ResourceGroupName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public string VMSize { get; set; } = string.Empty;
    public string VMFamily { get; set; } = string.Empty;

    public int? VCPUs { get; set; }
    public decimal? MemoryGB { get; set; }

    public string OSType { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Offer { get; set; } = string.Empty;
    public string ImageSku { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    public int? NICCount { get; set; }

    public string OSDiskName { get; set; } = string.Empty;
    public string OSDiskType { get; set; } = string.Empty;
    public decimal? OSDiskSizeGB { get; set; }

    public int? DataDiskCount { get; set; }
    public decimal? TotalDataDiskGB { get; set; }

    public List<VirtualMachineDiskModel> Disks { get; set; } = [];

    public string DisplayPowerState =>
        string.IsNullOrWhiteSpace(PowerState)
            ? ProvisioningState
            : PowerState;

    public int DisplayVCPUs => VCPUs ?? 0;

    public decimal DisplayMemoryGB => MemoryGB ?? 0;

    public string DisplayOSType =>
        string.IsNullOrWhiteSpace(OSType)
            ? "No identificado"
            : OSType;

    public int DisplayDiskCount => Disks.Count;

    public decimal DisplayDiskSizeGB =>
        Disks.Sum(disk => disk.DiskSizeGB);
}
