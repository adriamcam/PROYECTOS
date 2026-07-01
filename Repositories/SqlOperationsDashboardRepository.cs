using System.Data;
using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class SqlOperationsDashboardRepository : ISqlOperationsDashboardRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlOperationsDashboardRepository(ISqlConnectionFactory connectionFactory)
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

    public async Task<SqlOperationsDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var result = await connection.QueryFirstOrDefaultAsync<SqlOperationsDashboardModel>(
            new CommandDefinition(
                "dbo.ITQS_SOC_sp_SQLOperations_GetDashboard",
                commandType: CommandType.StoredProcedure,
                commandTimeout: 180,
                cancellationToken: cancellationToken));

        return result ?? new SqlOperationsDashboardModel();
    }
}
