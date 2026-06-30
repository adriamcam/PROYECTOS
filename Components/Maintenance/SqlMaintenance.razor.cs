using System.Security.Claims;
using ITQS.SupportOperationsCenter.Models.Maintenance;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace ITQS.SupportOperationsCenter.Components.Maintenance;

public partial class SqlMaintenance : ComponentBase
{
    [Inject] private ISqlMaintenanceService SqlMaintenanceService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    protected bool IsLoading { get; set; } = true;
    protected bool IsExecuting { get; set; }
    protected bool CanAccess { get; set; }
    protected int RetentionDays { get; set; } = 30;
    protected string UserEmail { get; set; } = string.Empty;
    protected string UserName { get; set; } = string.Empty;
    protected SqlMaintenanceDashboardModel Dashboard { get; set; } = new();
    protected SqlMaintenanceExecutionResultModel? LastExecution { get; set; }
    protected SqlMaintenanceTableSummaryModel? PreviewTable { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        UserEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value ?? user.FindFirst("preferred_username")?.Value ?? user.Identity?.Name ?? string.Empty;
        UserName = user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("name")?.Value ?? user.Identity?.Name ?? UserEmail;
        CanAccess = await SqlMaintenanceService.CanAccessAsync(UserEmail);
        if (CanAccess) await LoadDashboardAsync();
        IsLoading = false;
    }

    protected async Task LoadDashboardAsync()
    {
        IsExecuting = true;
        try { Dashboard = await SqlMaintenanceService.GetDashboardAsync(RetentionDays); Dashboard.LastExecution = LastExecution; }
        finally { IsExecuting = false; }
    }

    protected Task OpenPreviewAsync(SqlMaintenanceTableSummaryModel table) { PreviewTable = table; return Task.CompletedTask; }
    protected void ClosePreview() => PreviewTable = null;

    protected async Task ExecuteCleanupFromPreviewAsync()
    {
        if (PreviewTable is null) return;
        var table = PreviewTable;
        PreviewTable = null;
        await ExecuteCleanupAsync(table);
    }

    protected async Task ExecuteCleanupAsync(SqlMaintenanceTableSummaryModel table)
    {
        if (table.IsProtected || table.EligibleToDelete <= 0) return;
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", $"Se eliminarán {table.EligibleToDelete:N0} registros de {table.DisplayName}. ¿Desea continuar?");
        if (!confirmed) return;
        IsExecuting = true;
        try
        {
            var request = BuildRequest(table.TableName);
          LastExecution = table.TableName switch
{
    "AlertsManagement" => await SqlMaintenanceService.CleanupAlertsManagementAsync(request),
    "AzureAlertCloseQueue" => await SqlMaintenanceService.CleanupAzureAlertCloseQueueAsync(request),
    "AlertasBackup" => await SqlMaintenanceService.CleanupAlertasBackupAsync(request),
    "AlertUpdatesHistory" => await SqlMaintenanceService.CleanupAlertUpdatesHistoryAsync(request),
    _ => new SqlMaintenanceExecutionResultModel
    {
        TableName = table.TableName,
        Succeeded = false,
        Message = "Tabla no soportada por el módulo.",
        StartedAt = DateTime.Now,
        FinishedAt = DateTime.Now,
        ExecutedBy = UserName,
        ExecutedByEmail = UserEmail
    }
};
        }
        finally { IsExecuting = false; }
    }

    protected static string GetIcon(string tableName) => tableName switch { "AlertsManagement" => "🔔", "AzureAlertCloseQueue" => "✉️", "AlertasBackup" => "🗄️", "AlertUpdatesHistory" => "🕘", _ => "🧩" };
    protected static string GetIconClass(string tableName) => tableName switch { "AlertsManagement" => "icon-red", "AzureAlertCloseQueue" => "icon-blue", "AlertasBackup" => "icon-orange", "AlertUpdatesHistory" => "icon-green", _ => "icon-blue" };

    private SqlMaintenanceRequestModel BuildRequest(string tableName) => new() { TableName = tableName, RetentionDays = RetentionDays, BatchSize = 5000, UserEmail = UserEmail, UserName = UserName };
}
