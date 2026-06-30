using ITQS.SupportOperationsCenter.Models.Maintenance;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;
namespace ITQS.SupportOperationsCenter.Services;
public sealed class SqlMaintenanceService : ISqlMaintenanceService
{
    private readonly ISqlMaintenanceRepository _repository; private readonly ILogger<SqlMaintenanceService> _logger;
    public SqlMaintenanceService(ISqlMaintenanceRepository repository, ILogger<SqlMaintenanceService> logger){_repository=repository;_logger=logger;}
    public async Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken=default){try{return await _repository.CanAccessAsync(userEmail,cancellationToken);}catch(Exception ex){_logger.LogError(ex,"Error validating SQL maintenance access.");return false;}}
    public async Task<SqlMaintenanceDashboardModel> GetDashboardAsync(int retentionDays,CancellationToken cancellationToken=default){try{return await _repository.GetDashboardAsync(NormalizeRetentionDays(retentionDays),cancellationToken);}catch(Exception ex){_logger.LogError(ex,"Error loading SQL maintenance dashboard.");return new SqlMaintenanceDashboardModel{RetentionDays=NormalizeRetentionDays(retentionDays)};}}
    public Task<SqlMaintenanceExecutionResultModel> CleanupAlertsManagementAsync(SqlMaintenanceRequestModel r,CancellationToken c=default){r=Normalize(r);return Safe(()=>_repository.CleanupAlertsManagementAsync(r,c),r);}    
    public Task<SqlMaintenanceExecutionResultModel> CleanupAzureAlertCloseQueueAsync(SqlMaintenanceRequestModel r,CancellationToken c=default){r=Normalize(r);return Safe(()=>_repository.CleanupAzureAlertCloseQueueAsync(r,c),r);}    
    public Task<SqlMaintenanceExecutionResultModel> CleanupAlertasBackupAsync(SqlMaintenanceRequestModel r,CancellationToken c=default){r=Normalize(r);return Safe(()=>_repository.CleanupAlertasBackupAsync(r,c),r);}    
    public Task<SqlMaintenanceExecutionResultModel> UpdateStatisticsAsync(SqlMaintenanceRequestModel r,CancellationToken c=default){r=Normalize(r);return Safe(()=>_repository.UpdateStatisticsAsync(r,c),r);}    
    public Task<SqlMaintenanceExecutionResultModel> RebuildIndexesAsync(SqlMaintenanceRequestModel r,CancellationToken c=default){r=Normalize(r);return Safe(()=>_repository.RebuildIndexesAsync(r,c),r);}    
    private async Task<SqlMaintenanceExecutionResultModel> Safe(Func<Task<SqlMaintenanceExecutionResultModel>> a, SqlMaintenanceRequestModel r){try{return await a();}catch(Exception ex){_logger.LogError(ex,"Error executing SQL maintenance action {ActionName}.",r.ActionName);return new SqlMaintenanceExecutionResultModel{TableName=r.TableName,ActionName=r.ActionName,RetentionDays=r.RetentionDays,BatchSize=r.BatchSize,Succeeded=false,Message=ex.Message,StartedAt=DateTime.Now,FinishedAt=DateTime.Now,ExecutedBy=r.UserName,ExecutedByEmail=r.UserEmail};}}
    private static SqlMaintenanceRequestModel Normalize(SqlMaintenanceRequestModel r){r.RetentionDays=NormalizeRetentionDays(r.RetentionDays);r.BatchSize=r.BatchSize<=0?5000:Math.Min(r.BatchSize,20000);r.UserEmail=r.UserEmail?.Trim()??string.Empty;r.UserName=r.UserName?.Trim()??string.Empty;return r;}
    private static int NormalizeRetentionDays(int d){if(d<30)return 30;if(d>365)return 365;return d;}
}
