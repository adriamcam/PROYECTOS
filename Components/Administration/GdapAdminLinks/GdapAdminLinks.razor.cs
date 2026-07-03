using System.Security.Claims;
using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

namespace ITQS.SupportOperationsCenter.Components.Administration.GdapAdminLinks;

public partial class GdapAdminLinks : ComponentBase
{
    [Inject] private IGdapAdminLinksService GdapService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected bool IsLoading { get; set; } = true;
    protected string UserEmail { get; set; } = string.Empty;
    protected string Message { get; set; } = string.Empty;
    protected string MessageCss { get; set; } = "gdap-message ok";
    protected string ActiveTab { get; set; } = "Dashboard";

    protected GdapAdminLinksDashboardModel Dashboard { get; set; } = new();
    protected GdapAdminLinksFilterModel Filters { get; set; } = new();

    protected List<GdapAdminLinksCustomerModel> Items { get; set; } = new();
    protected List<GdapAdminLinksCustomerModel> PendingEmailItems { get; set; } = new();
    protected List<GdapAdminLinksCustomerModel> ExpiringItems { get; set; } = new();

    protected bool ShowDetail { get; set; }
    protected GdapAdminLinksCustomerModel? SelectedDetail { get; set; }

    protected bool ShowEdit { get; set; }
    protected string EditCustomerName { get; set; } = string.Empty;
    protected GdapAdminLinksSaveCustomerRequest EditRequest { get; set; } = new();

    protected int PageNumber { get; set; } = 1;
    protected int PageSize { get; set; } = 10;

    protected List<GdapAdminLinksCustomerModel> FilteredItems => Items.ToList();

    protected int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredItems.Count / (double)PageSize));

    protected List<GdapAdminLinksCustomerModel> PagedItems =>
        FilteredItems.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    protected int FirstItemNumber => FilteredItems.Count == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    protected int LastItemNumber => Math.Min(PageNumber * PageSize, FilteredItems.Count);

    protected int HealthPercent => Dashboard.TotalCustomers <= 0
        ? 0
        : (int)Math.Round((Dashboard.ActiveGdap / (double)Dashboard.TotalCustomers) * 100, 0);

    protected IEnumerable<int> VisiblePages
    {
        get
        {
            var start = Math.Max(1, PageNumber - 2);
            var end = Math.Min(TotalPages, start + 4);
            start = Math.Max(1, end - 4);
            return Enumerable.Range(start, end - start + 1);
        }
    }

    protected IEnumerable<string> PartnerOptions =>
        Items.Select(x => x.PartnerTenant).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x);

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        UserEmail = user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.Identity?.Name
            ?? string.Empty;

        await RefreshAsync();
    }

    protected async Task RefreshAsync()
    {
        IsLoading = true;
        Message = string.Empty;

        try
        {
            Dashboard = await GdapService.GetDashboardAsync();
            await LoadItemsAsync();
            await LoadPendingEmailsAsync();
            await LoadExpiringAsync();
        }
        catch (Exception ex)
        {
            SetError($"Error cargando Admin Links GDAP: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task ChangeTabAsync(string tab)
    {
        ActiveTab = tab;

        if (tab == "Mail")
            await LoadPendingEmailsAsync();
        else if (tab == "Expiring")
            await LoadExpiringAsync();
    }

    protected async Task LoadItemsAsync()
    {
        Items = (await GdapService.GetCustomersAsync(Filters)).ToList();
        EnsureValidPage();
    }

    protected async Task LoadPendingEmailsAsync()
    {
        PendingEmailItems = (await GdapService.GetPendingEmailsAsync()).ToList();
    }

    protected async Task LoadExpiringAsync()
    {
        ExpiringItems = (await GdapService.GetExpiringSoonAsync()).ToList();
    }

    protected async Task ApplyFiltersAsync()
    {
        PageNumber = 1;
        await LoadItemsAsync();
    }

    protected async Task SearchKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
            await ApplyFiltersAsync();
    }

    protected async Task OpenDetailAsync(int id)
    {
        SelectedDetail = await GdapService.GetCustomerAsync(id);
        ShowDetail = SelectedDetail is not null;
    }

    protected void CloseDetail()
    {
        ShowDetail = false;
        SelectedDetail = null;
    }

    protected void OpenEditAsync(GdapAdminLinksCustomerModel item)
    {
        EditCustomerName = item.CustomerName;
        EditRequest = new GdapAdminLinksSaveCustomerRequest
        {
            Id = item.Id,
            CustomerTenantId = item.CustomerTenantId,
            PrimaryContactName = item.PrimaryContactName,
            PrimaryEmail = item.PrimaryEmail,
            CCEmails = item.CCEmails,
            AutoSendEmail = item.AutoSendEmail,
            IsActive = item.IsActive,
            ExcludeReason = item.ExcludeReason,
            UpdatedBy = UserEmail
        };
        ShowEdit = true;
    }

    protected void CloseEdit()
    {
        ShowEdit = false;
    }

    protected async Task SaveCustomerAsync()
    {
        EditRequest.UpdatedBy = UserEmail;
        var result = await GdapService.UpdateCustomerAsync(EditRequest);

        if (result.Success)
        {
            SetOk(result.Message);
            ShowEdit = false;
            await RefreshAsync();
        }
        else
        {
            SetError(result.ErrorMessage);
        }
    }

    protected async Task DisableAsync(GdapAdminLinksCustomerModel item)
    {
        var result = await GdapService.DisableCustomerAsync(item.Id, UserEmail, "Cliente desactivado desde Admin Links GDAP.");

        if (result.Success)
        {
            SetOk(result.Message);
            await RefreshAsync();
        }
        else
        {
            SetError(result.ErrorMessage);
        }
    }

    protected async Task EnableAsync(GdapAdminLinksCustomerModel item)
    {
        var result = await GdapService.EnableCustomerAsync(item.Id, UserEmail);

        if (result.Success)
        {
            SetOk(result.Message);
            await RefreshAsync();
        }
        else
        {
            SetError(result.ErrorMessage);
        }
    }

    protected void OpenApprovalUrl(GdapAdminLinksCustomerModel item)
    {
        if (string.IsNullOrWhiteSpace(item.ApprovalPendingLink))
        {
            SetError("El cliente no tiene Approval URL generado.");
            return;
        }

        NavigationManager.NavigateTo(item.ApprovalPendingLink, forceLoad: true);
    }

    protected async Task GoToPageAsync(int page)
    {
        PageNumber = Math.Clamp(page, 1, TotalPages);
        await Task.CompletedTask;
    }

    protected async Task PreviousPageAsync()
    {
        if (PageNumber > 1) PageNumber--;
        await Task.CompletedTask;
    }

    protected async Task NextPageAsync()
    {
        if (PageNumber < TotalPages) PageNumber++;
        await Task.CompletedTask;
    }

    protected async Task PageSizeChangedAsync()
    {
        PageNumber = 1;
        EnsureValidPage();
        await Task.CompletedTask;
    }

    private void EnsureValidPage()
    {
        if (PageNumber > TotalPages) PageNumber = TotalPages;
        if (PageNumber < 1) PageNumber = 1;
    }

    protected static string FormatDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("dd/MM/yyyy") : "-";

    protected static string FormatDateTime(DateTime? value)
        => value.HasValue ? value.Value.ToString("dd/MM/yyyy HH:mm") : "-";

    protected static string StatusText(GdapAdminLinksCustomerModel item)
        => string.IsNullOrWhiteSpace(item.StatusFound) ? "Sin estado" : item.StatusFound;

    protected static string StatusCss(GdapAdminLinksCustomerModel item)
    {
        var status = item.StatusFound?.ToLowerInvariant() ?? string.Empty;

        if (!item.IsActive) return "disabled";
        if (status.Contains("approvalpending")) return "pending";
        if (status.Contains("active")) return "active";
        if (status.Contains("expired") || status.Contains("terminated") || status.Contains("sin gdap")) return "danger";
        return "neutral";
    }

    protected static string DaysCss(GdapAdminLinksCustomerModel item)
    {
        if (!item.DaysToExpire.HasValue) return "days-neutral";
        if (item.DaysToExpire.Value <= 5) return "days-danger";
        if (item.DaysToExpire.Value <= 15) return "days-high";
        if (item.DaysToExpire.Value <= 30) return "days-warning";
        return "days-ok";
    }

    private void SetOk(string message)
    {
        Message = message;
        MessageCss = "gdap-message ok";
    }

    private void SetError(string message)
    {
        Message = message;
        MessageCss = "gdap-message error";
    }
}
