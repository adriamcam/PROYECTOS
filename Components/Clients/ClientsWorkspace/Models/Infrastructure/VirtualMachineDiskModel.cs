namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Infrastructure;

public sealed class VirtualMachineDiskModel
{
    public string VMResourceId { get; set; } = string.Empty;
    public string VMName { get; set; } = string.Empty;
    public string DiskResourceId { get; set; } = string.Empty;
    public string DiskName { get; set; } = string.Empty;
    public string DiskRole { get; set; } = string.Empty;

    public int? LUN { get; set; }

    public string DiskType { get; set; } = string.Empty;
    public decimal? DiskSizeGB { get; set; }

    public string ManagedDiskType { get; set; } = string.Empty;
    public string StorageAccountType { get; set; } = string.Empty;
    public string Caching { get; set; } = string.Empty;

    public string DisplayRole =>
        string.IsNullOrWhiteSpace(DiskRole)
            ? "DISK"
            : DiskRole.ToUpperInvariant();

    public string DisplayTier
    {
        get
        {
            var value = FirstNotEmpty(
                ManagedDiskType,
                StorageAccountType,
                DiskType);

            return value switch
            {
                "Premium_LRS" => "Premium SSD",
                "PremiumV2_LRS" => "Premium SSD v2",
                "StandardSSD_LRS" => "Standard SSD",
                "Standard_LRS" => "Standard HDD",
                "UltraSSD_LRS" => "Ultra Disk",
                "LocalTemporary" => "Local temporal",
                _ => value
            };
        }
    }

    private static string FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(
                   value => !string.IsNullOrWhiteSpace(value))
               ?? string.Empty;
    }
}
