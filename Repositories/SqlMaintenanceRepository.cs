using System.Data;
using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Maintenance;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class SqlMaintenanceRepository : ISqlMaintenanceRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlMaintenanceRepository> _logger;

    public SqlMaintenanceRepository(
        ISqlConnectionFactory connectionFactory,
        ILogger<SqlMaintenanceRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
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

    public async Task<SqlMaintenanceDashboardModel> GetDashboardAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@RetentionDays", retentionDays, DbType.Int32);

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                "dbo.ITQS_SOC_sp_SQLMaintenance_GetDashboard",
                p,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 300,
                cancellationToken: cancellationToken));

        var summary = await multi.ReadFirstOrDefaultAsync<SqlMaintenanceDashboardModel>()
                      ?? new SqlMaintenanceDashboardModel();

        summary.Tables = (await multi.ReadAsync<SqlMaintenanceTableSummaryModel>()).ToList();
        summary.History = (await multi.ReadAsync<SqlMaintenanceHistoryModel>()).ToList();
        summary.RetentionDays = retentionDays;

        return summary;
    }

    public Task<SqlMaintenanceExecutionResultModel> CleanupAlertsManagementAsync(
        SqlMaintenanceRequestModel request,
        CancellationToken cancellationToken = default)
    {
        request.TableName = "AlertsManagement";
        request.ActionName = "CleanupClosedOlderThanRetention";

        return Execute(
            "dbo.ITQS_SOC_sp_SQLMaintenance_CleanupAlertsManagement",
            request,
            cancellationToken);
    }

    public Task<SqlMaintenanceExecutionResultModel> CleanupAzureAlertCloseQueueAsync(
        SqlMaintenanceRequestModel request,
        CancellationToken cancellationToken = default)
    {
        request.TableName = "AzureAlertCloseQueue";
        request.ActionName = "CleanupQueueOlderThanRetention";

        return Execute(
            "dbo.ITQS_SOC_sp_SQLMaintenance_CleanupAzureAlertCloseQueue",
            request,
            cancellationToken);
    }

    public Task<SqlMaintenanceExecutionResultModel> CleanupAlertasBackupAsync(
        SqlMaintenanceRequestModel request,
        CancellationToken cancellationToken = default)
    {
        request.TableName = "AlertasBackup";
        request.ActionName = "CleanupInactiveOlderThanRetention";

        return Execute(
            "dbo.ITQS_SOC_sp_SQLMaintenance_CleanupAlertasBackup",
            request,
            cancellationToken);
    }

    public Task<SqlMaintenanceExecutionResultModel> CleanupAlertUpdatesHistoryAsync(
        SqlMaintenanceRequestModel request,
        CancellationToken cancellationToken = default)
    {
        request.TableName = "AlertUpdatesHistory";
        request.ActionName = "CleanupHistoryOlderThanRetention";

        return Execute(
            "dbo.ITQS_SOC_sp_SQLMaintenance_CleanupAlertUpdatesHistory",
            request,
            cancellationToken);
    }

    private async Task<SqlMaintenanceExecutionResultModel> Execute(
        string sp,
        SqlMaintenanceRequestModel request,
        CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@RetentionDays", request.RetentionDays);
        p.Add("@BatchSize", request.BatchSize);
        p.Add("@UserEmail", request.UserEmail);
        p.Add("@UserName", request.UserName);

        var result = await connection.QueryFirstOrDefaultAsync<SqlMaintenanceExecutionResultModel>(
            new CommandDefinition(
                sp,
                p,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 600,
                cancellationToken: cancellationToken));

        return result ?? new SqlMaintenanceExecutionResultModel
        {
            TableName = request.TableName,
            ActionName = request.ActionName,
            RetentionDays = request.RetentionDays,
            BatchSize = request.BatchSize,
            Succeeded = false,
            Message = "El procedimiento no devolvió resultado.",
            StartedAt = DateTime.Now,
            FinishedAt = DateTime.Now,
            ExecutedBy = request.UserName,
            ExecutedByEmail = request.UserEmail
        };
    }
}