namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Infrastructure;

public sealed class VirtualMachineModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;

    public string VMName { get; set; } = string.Empty;

    public string PowerState { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string VMState { get; set; } = string.Empty;

    public string ResourceGroupName { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    public string VMSize { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;

    public int? VCPUs { get; set; }
    public int? VCPU { get; set; }
    public int? VCpuCount { get; set; }

    public decimal? MemoryGB { get; set; }
    public decimal? RAMGB { get; set; }
    public decimal? RamGB { get; set; }

    public string OSType { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string OSName { get; set; } = string.Empty;
    public string OSFamily { get; set; } = string.Empty;

    public string PrimaryPrivateIPAddress { get; set; } = string.Empty;
    public string PrivateIPAddress { get; set; } = string.Empty;
    public string PrivateIP { get; set; } = string.Empty;

    public string PrimaryNicName { get; set; } = string.Empty;
    public string NICName { get; set; } = string.Empty;
    public string NicName { get; set; } = string.Empty;

    public int? TotalDiskCount { get; set; }
    public int? DiskCount { get; set; }
    public int? TotalDisks { get; set; }

    public decimal? TotalDiskSizeGB { get; set; }
    public decimal? DiskSizeGB { get; set; }
    public decimal? TotalStorageGB { get; set; }

    public List<VirtualMachineDiskModel> Disks { get; set; } = [];

    public string DisplayPowerState =>
        FirstNotEmpty(PowerState, State, VMState);

    public string DisplayResourceGroup =>
        FirstNotEmpty(ResourceGroupName, ResourceGroup);

    public string DisplayLocation =>
        FirstNotEmpty(Location, Region);

    public string DisplayVMSize =>
        FirstNotEmpty(VMSize, Size);

    public int DisplayVCPUs =>
        VCPUs ?? VCPU ?? VCpuCount ?? 0;

    public decimal DisplayMemoryGB =>
        MemoryGB ?? RAMGB ?? RamGB ?? 0;

    public string DisplayOSType =>
        FirstNotEmpty(OSType, OperatingSystem, OSName, OSFamily);

    public string DisplayPrivateIP =>
        FirstNotEmpty(
            PrimaryPrivateIPAddress,
            PrivateIPAddress,
            PrivateIP);

    public string DisplayNicName =>
        FirstNotEmpty(
            PrimaryNicName,
            NICName,
            NicName);

    public int DisplayDiskCount =>
        Disks.Count > 0
            ? Disks.Count
            : TotalDiskCount ?? DiskCount ?? TotalDisks ?? 0;

    public decimal DisplayDiskSizeGB =>
        Disks.Count > 0
            ? Disks.Sum(disk => disk.DiskSizeGB ?? 0)
            : TotalDiskSizeGB ?? DiskSizeGB ?? TotalStorageGB ?? 0;

    private static string FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(
                   value => !string.IsNullOrWhiteSpace(value))
               ?? string.Empty;
    }
}
