using System.Data;
using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Administration.Customers;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class CustomerAdminRepository : ICustomerAdminRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CustomerAdminRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userEmail)) return false;

        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
SELECT TOP (1)
CASE
    WHEN ISNULL(IsActive,0)=1
     AND (
            UPPER(ISNULL(EffectiveRole,''))='ADMIN'
         OR UPPER(ISNULL(BaseRole,''))='ADMIN'
         OR ISNULL(IsTempAdmin,0)=1
         )
    THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END
FROM dbo.ITQS_AppUsers
WHERE LOWER(LTRIM(RTRIM(UserEmail))) = LOWER(LTRIM(RTRIM(@UserEmail)));";

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserEmail = userEmail }, cancellationToken: cancellationToken));
    }

    public async Task<CustomerAdminDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryFirstAsync<CustomerAdminDashboardModel>(
            new CommandDefinition("dbo.ITQS_SOC_sp_CustomerAdmin_GetDashboard",
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120,
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<CustomerAdminModel>> GetCustomersAsync(string searchText, string status, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@SearchText", searchText?.Trim() ?? string.Empty);
        p.Add("@Status", status?.Trim() ?? "All");

        var rows = await connection.QueryAsync<CustomerAdminModel>(
            new CommandDefinition("dbo.ITQS_SOC_sp_CustomerAdmin_GetCustomers",
                p,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 180,
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<CustomerAdminModel> SaveCustomerAsync(CustomerAdminSaveRequestModel request, Guid? originalTenantId = null, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@OriginalTenantId", originalTenantId);
        p.Add("@TenantId", request.TenantId);
        p.Add("@CustomerName", request.CustomerName?.Trim());
        p.Add("@CustomerNamePortal", request.CustomerNamePortal?.Trim());
        p.Add("@ClientId", request.ClientId?.Trim());
        p.Add("@SecretName", request.SecretName?.Trim());
        p.Add("@IsActive", request.IsActive);
        p.Add("@Source", string.IsNullOrWhiteSpace(request.Source) ? "SupportCloud" : request.Source.Trim());
        p.Add("@Notes", request.Notes?.Trim());
        p.Add("@UpdatedBy", request.UpdatedBy?.Trim());

        return await connection.QueryFirstAsync<CustomerAdminModel>(
            new CommandDefinition("dbo.ITQS_SOC_sp_CustomerAdmin_SaveCustomer",
                p,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 180,
                cancellationToken: cancellationToken));
    }

    public async Task DeleteCustomerAsync(Guid tenantId, string userEmail, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
UPDATE dbo.ITQS_Customers
SET
    IsActive = 0,
    Notes = CASE
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(Notes,''))), '') IS NULL
            THEN CONCAT('Eliminado desde Support Cloud por ', @UserEmail, ' el ', CONVERT(varchar(19), SYSUTCDATETIME(), 120), ' UTC')
        ELSE CONCAT(Notes, CHAR(13), CHAR(10), 'Eliminado desde Support Cloud por ', @UserEmail, ' el ', CONVERT(varchar(19), SYSUTCDATETIME(), 120), ' UTC')
    END,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId;

SELECT @@ROWCOUNT;";

        var affected = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql,
                new { TenantId = tenantId, UserEmail = userEmail },
                commandTimeout: 120,
                cancellationToken: cancellationToken));

        if (affected <= 0)
            throw new InvalidOperationException("No se encontró el cliente para eliminar.");
    }

}
