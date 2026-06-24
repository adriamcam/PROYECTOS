using Dapper;
using ITQS.SupportOperationsCenter.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ITQS.SupportOperationsCenter.Services;

public interface IAlertService
{
    Task<AlertDashboardDto> GetDashboardAsync(string userEmail);
    Task<IReadOnlyList<AlertListItemDto>> GetAssignedAlertsAsync(string userEmail, int pageNumber, int pageSize, string? search);
    Task<IReadOnlyList<AlertListItemDto>> GetUnassignedAlertsAsync(int pageNumber, int pageSize, string? search);
    Task<AlertDetailResultDto> GetAlertDetailAsync(long id);
    Task AssignAlertAsync(long id, string userName, string userEmail, string comment);
    Task CloseAlertAsync(long id, string userName, string userEmail, string comment);
}

public sealed class AlertService : IAlertService
{
    private readonly IConfiguration _configuration;

    public AlertService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("ReportesDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:ReportesDb no está configurado.");
        }
        return new SqlConnection(connectionString);
    }

    public async Task<AlertDashboardDto> GetDashboardAsync(string userEmail)
    {
        await using var cn = CreateConnection();
        var result = await cn.QuerySingleOrDefaultAsync<AlertDashboardDto>(
            "dbo.sp_App_GetAlertDashboard",
            new { UserEmail = userEmail },
            commandType: CommandType.StoredProcedure);
        return result ?? new AlertDashboardDto();
    }

    public async Task<IReadOnlyList<AlertListItemDto>> GetAssignedAlertsAsync(string userEmail, int pageNumber, int pageSize, string? search)
    {
        await using var cn = CreateConnection();
        var rows = await cn.QueryAsync<AlertListItemDto>(
            "dbo.sp_App_GetAssignedAlerts",
            new { UserEmail = userEmail, PageNumber = pageNumber, PageSize = Math.Min(pageSize, 50), Search = search },
            commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<AlertListItemDto>> GetUnassignedAlertsAsync(int pageNumber, int pageSize, string? search)
    {
        await using var cn = CreateConnection();
        var rows = await cn.QueryAsync<AlertListItemDto>(
            "dbo.sp_App_GetUnassignedAlerts",
            new { PageNumber = pageNumber, PageSize = Math.Min(pageSize, 50), Search = search },
            commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<AlertDetailResultDto> GetAlertDetailAsync(long id)
    {
        await using var cn = CreateConnection();
        using var multi = await cn.QueryMultipleAsync(
            "dbo.sp_App_GetAlertDetail",
            new { Id = id },
            commandType: CommandType.StoredProcedure);

        var alert = await multi.ReadSingleOrDefaultAsync<AlertListItemDto>();
        var history = (await multi.ReadAsync<AlertHistoryDto>()).ToList();
        return new AlertDetailResultDto { Alert = alert, History = history };
    }

    public async Task AssignAlertAsync(long id, string userName, string userEmail, string comment)
    {
        await using var cn = CreateConnection();
        await cn.ExecuteAsync(
            "dbo.sp_App_AssignAlert",
            new { Id = id, UserName = userName, UserEmail = userEmail, Comment = comment },
            commandType: CommandType.StoredProcedure);
    }

    public async Task CloseAlertAsync(long id, string userName, string userEmail, string comment)
    {
        await using var cn = CreateConnection();
        await cn.ExecuteAsync(
            "dbo.sp_App_CloseAlert",
            new { Id = id, UserName = userName, UserEmail = userEmail, Comment = comment },
            commandType: CommandType.StoredProcedure);
    }
}
