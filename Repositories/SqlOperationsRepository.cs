using System.Data;
using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class SqlOperationsRepository : ISqlOperationsRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlOperationsRepository(ISqlConnectionFactory connectionFactory)
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
     AND (UPPER(ISNULL(EffectiveRole,''))='ADMIN'
       OR UPPER(ISNULL(BaseRole,''))='ADMIN'
       OR ISNULL(IsTempAdmin,0)=1)
    THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END
FROM dbo.ITQS_AppUsers
WHERE LOWER(LTRIM(RTRIM(UserEmail))) = LOWER(LTRIM(RTRIM(@UserEmail)));";

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserEmail = userEmail }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<SqlJobModel>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<SqlJobModel>(
            new CommandDefinition("dbo.ITQS_SOC_sp_SQLMaintenance_GetJobs", commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SqlBlockingModel>> GetBlockingAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<SqlBlockingModel>(
            new CommandDefinition("dbo.ITQS_SOC_sp_SQLMaintenance_GetBlocking", commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SqlSessionModel>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<SqlSessionModel>(
            new CommandDefinition("dbo.ITQS_SOC_sp_SQLMaintenance_GetActiveSessions", commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SqlSlowQueryModel>> GetSlowQueriesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<SqlSlowQueryModel>(
            new CommandDefinition("dbo.ITQS_SOC_sp_SQLMaintenance_GetSlowQueries", commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: cancellationToken));
        return rows.ToList();
    }
}
