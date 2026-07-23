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
            SELECT *
            FROM dbo.vw_VMInfrastructure
            WHERE TRY_CONVERT(
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
                Caching
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

        foreach (var vm in virtualMachines)
        {
            vm.Disks = disks
                .Where(disk =>
                    (!string.IsNullOrWhiteSpace(vm.ResourceId) &&
                     string.Equals(
                         disk.VMResourceId,
                         vm.ResourceId,
                         StringComparison.OrdinalIgnoreCase))
                    ||
                    string.Equals(
                        disk.VMName,
                        vm.VMName,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return virtualMachines;
    }
}
