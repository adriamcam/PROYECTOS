using System.Data;
using Dapper;
using ITQS.SupportOperationsCenter.Models;

namespace ITQS.SupportOperationsCenter.Services;

public interface IAlertService
{
    Task<AlertDashboardDto> GetDashboardAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertListItemDto>> GetAssignedAlertsAsync(string userEmail, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertListItemDto>> GetUnassignedAlertsAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<AlertDetailResultDto> GetAlertDetailAsync(long alertRecordId, CancellationToken cancellationToken = default);
    Task AssignAlertAsync(long alertRecordId, string userName, string userEmail, string? comment, CancellationToken cancellationToken = default);
    Task CloseAlertAsync(long alertRecordId, string userName, string userEmail, string? comment, CancellationToken cancellationToken = default);
}

public sealed class AlertService(ISqlConnectionFactory connectionFactory) : IAlertService
{
    private const int MaxPageSize = 50;

    public async Task<AlertDashboardDto> GetDashboardAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition("dbo.sp_App_GetAlertDashboard", new { UserEmail = userEmail }, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<AlertDashboardDto>(command);
    }

    public async Task<IReadOnlyList<AlertListItemDto>> GetAssignedAlertsAsync(string userEmail, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition("dbo.sp_App_GetAssignedAlerts", new
        {
            UserEmail = userEmail,
            PageNumber = Math.Max(pageNumber, 1),
            PageSize = Math.Clamp(pageSize, 1, MaxPageSize),
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()
        }, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<AlertListItemDto>(command)).AsList();
    }

    public async Task<IReadOnlyList<AlertListItemDto>> GetUnassignedAlertsAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition("dbo.sp_App_GetUnassignedAlerts", new
        {
            PageNumber = Math.Max(pageNumber, 1),
            PageSize = Math.Clamp(pageSize, 1, MaxPageSize),
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()
        }, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<AlertListItemDto>(command)).AsList();
    }

    public async Task<AlertDetailResultDto> GetAlertDetailAsync(long alertRecordId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition("dbo.sp_App_GetAlertDetail", new { AlertRecordId = alertRecordId }, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
        using var multi = await connection.QueryMultipleAsync(command);
        var alert = await multi.ReadSingleOrDefaultAsync<AlertDetailDto>();
        var history = (await multi.ReadAsync<AlertHistoryDto>()).AsList();
        return new AlertDetailResultDto { Alert = alert, History = history };
    }

    public async Task AssignAlertAsync(long alertRecordId, string userName, string userEmail, string? comment, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition("dbo.sp_App_AssignAlert", new { AlertRecordId = alertRecordId, UserName = userName, UserEmail = userEmail, Comment = comment }, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task CloseAlertAsync(long alertRecordId, string userName, string userEmail, string? comment, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition("dbo.sp_App_CloseAlert", new { AlertRecordId = alertRecordId, UserName = userName, UserEmail = userEmail, Comment = comment }, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
