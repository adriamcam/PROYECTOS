using System.Security.Claims;
using ITQS.SupportOperationsCenter.Models.Maintenance.SqlOperations;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ITQS.SupportOperationsCenter.Components.Maintenance.SqlOperations;

public partial class SqlOperationsDashboard : ComponentBase
{
    [Inject] private ISqlOperationsDashboardService SqlOperationsDashboardService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter] public EventCallback<string> NavigateModule { get; set; }

    protected bool IsLoading { get; set; } = true;
    protected bool CanAccess { get; set; }
    protected string UserEmail { get; set; } = string.Empty;
    protected SqlOperationsDashboardModel Dashboard { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        UserEmail = user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.Identity?.Name
            ?? string.Empty;

        CanAccess = await SqlOperationsDashboardService.CanAccessAsync(UserEmail);

        if (CanAccess) await LoadAsync();

        IsLoading = false;
    }

    protected async Task LoadAsync()
    {
        IsLoading = true;
        try { Dashboard = await SqlOperationsDashboardService.GetDashboardAsync(); }
        finally { IsLoading = false; }
    }
}
