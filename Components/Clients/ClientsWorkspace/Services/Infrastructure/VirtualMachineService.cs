using Dapper;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Infrastructure;
using ITQS.SupportOperationsCenter.Data;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services.Infrastructure;

public sealed class VirtualMachineService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public VirtualMachineService(
        ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<VirtualMachineModel>> GetVirtualMachinesAsync(
        string customerName,
        string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return [];
        }

        const string vmSql = """
            SELECT
                CustomerName,
                CONVERT(nvarchar(36), TenantId) AS TenantId,
                SubscriptionId,
                SubscriptionName,
                ResourceId,
                VMName,
                ComputerName,
                PowerState,
                ProvisioningState,
                ResourceGroupName,
                Location,
                VMSize,
                VMFamily,
                VCPUs,
                MemoryGB,
                OSType,
                Publisher,
                Offer,
                ImageSku,
                Version,
                NICCount,
                OSDiskName,
                OSDiskType,
                OSDiskSizeGB,
                DataDiskCount,
                TotalDataDiskGB
            FROM dbo.VMInventoryCurrent
            WHERE IsActive = 1
              AND TRY_CONVERT(
                    uniqueidentifier,
                    SubscriptionId
                  ) = TRY_CONVERT(
                    uniqueidentifier,
                    @SubscriptionId
                  )
            ORDER BY VMName;
            """;

        const string diskSql = """
            SELECT
                VMResourceId,
                VMName,
                DiskResourceId,
                DiskName,
                DiskRole,
                LUN,
                DiskType,
                DiskSizeGB,
                ManagedDiskType,
                StorageAccountType,
                Caching,
                IOPSReadWrite,
                ThroughputMBpsReadWrite,
                EncryptionEnabled
            FROM dbo.VMDiskInventoryCurrent
            WHERE TRY_CONVERT(
                    uniqueidentifier,
                    SubscriptionId
                  ) = TRY_CONVERT(
                    uniqueidentifier,
                    @SubscriptionId
                  )
            ORDER BY
                VMName,
                CASE
                    WHEN DiskRole = 'OS' THEN 0
                    WHEN DiskRole = 'Data' THEN 1
                    WHEN DiskRole = 'Local' THEN 2
                    ELSE 3
                END,
                LUN,
                DiskName;
            """;

        using var connection =
            _connectionFactory.CreateConnection();

        connection.Open();

        var parameters = new
        {
            SubscriptionId = subscriptionId.Trim()
        };

        var virtualMachines =
            (await connection.QueryAsync<VirtualMachineModel>(
                vmSql,
                parameters))
            .ToList();

        var disks =
            (await connection.QueryAsync<VirtualMachineDiskModel>(
                diskSql,
                parameters))
            .ToList();

        var disksByVm =
            disks
                .Where(disk =>
                    !string.IsNullOrWhiteSpace(disk.VMResourceId))
                .GroupBy(
                    disk => disk.VMResourceId,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList(),
                    StringComparer.OrdinalIgnoreCase);

        foreach (var vm in virtualMachines)
        {
            if (!string.IsNullOrWhiteSpace(vm.ResourceId) &&
                disksByVm.TryGetValue(
                    vm.ResourceId,
                    out var vmDisks))
            {
                vm.Disks = vmDisks;
            }
        }

        return virtualMachines;
    }
}
