using System.Security.Claims;
using ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

namespace ITQS.SupportOperationsCenter.Components.Administration.AppRegistrationsITQS;

public partial class AppRegistrationsITQS : ComponentBase
{
    [Inject] private IAppRegistrationITQSService AppRegistrationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected bool IsLoading { get; set; } = true;
    protected string UserEmail { get; set; } = string.Empty;
    protected string Message { get; set; } = string.Empty;
    protected string MessageCss { get; set; } = "appreg-message ok";

    protected AppRegistrationDashboardModel Dashboard { get; set; } = new();
    protected AppRegistrationFilterModel Filters { get; set; } = new();

    protected List<AppRegistrationListItem> Items { get; set; } = new();
    protected List<AppRegistrationAssignableUserModel> AssignableUsers { get; set; } = new();

    protected bool ShowDetail { get; set; }
    protected AppRegistrationDetailModel? SelectedDetail { get; set; }

    protected bool ShowAssign { get; set; }
    protected AppRegistrationAssignRequest AssignRequest { get; set; } = new();
    protected string SelectedUserEmail { get; set; } = string.Empty;
    protected DateTime? RequiredDateText { get; set; } = DateTime.Today.AddDays(3);

    protected int PageNumber { get; set; } = 1;
    protected int PageSize { get; set; } = 10;

    protected List<AppRegistrationListItem> FilteredItems => Items.ToList();

    protected int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredItems.Count / (double)PageSize));

    protected List<AppRegistrationListItem> PagedItems =>
        FilteredItems.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    protected int FirstItemNumber => FilteredItems.Count == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    protected int LastItemNumber => Math.Min(PageNumber * PageSize, FilteredItems.Count);

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

    protected IEnumerable<string> CustomerOptions =>
        Items.Select(x => x.CustomerName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x);

    protected IEnumerable<AppRegistrationListItem> UpcomingExpirations =>
        Items.Where(x => x.DaysToExpire.HasValue && x.DaysToExpire.Value >= 0)
             .OrderBy(x => x.DaysToExpire)
             .Take(3);

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

        try
        {
            Dashboard = await AppRegistrationService.GetDashboardAsync();
            AssignableUsers = (await AppRegistrationService.GetAssignableUsersAsync()).ToList();
            await LoadItemsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task LoadItemsAsync()
    {
        Items = (await AppRegistrationService.GetListAsync(Filters)).ToList();
        EnsureValidPage();
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

    protected async Task OpenDetailAsync(long id)
    {
        SelectedDetail = await AppRegistrationService.GetDetailAsync(id);
        ShowDetail = SelectedDetail is not null;
    }

    protected void CloseDetail()
    {
        ShowDetail = false;
        SelectedDetail = null;
    }

    protected async Task OpenAssignAsync(AppRegistrationListItem item)
    {
        AssignRequest = new AppRegistrationAssignRequest
        {
            AppRegistrationId = item.Id,
            CustomerName = item.CustomerName,
            TenantId = item.TenantId,
            SubscriptionId = item.SubscriptionId,
            SubscriptionName = item.SubscriptionName,
            AppName = item.AppName,
            ClientId = item.ClientId,
            CredentialType = item.CredentialType,
            KeyId = item.KeyId,
            EndDate = item.EndDate,
            DaysToExpire = item.DaysToExpire,
            Priority = item.DaysToExpire <= 15 ? "Alta" : "Media",
            AssignedBy = UserEmail,
            RequiredDate = DateTime.Today.AddDays(3),
            Notes = "Generar nuevo Secret para la App Registration y actualizar en Key Vault."
        };

        RequiredDateText = AssignRequest.RequiredDate;
        SelectedUserEmail = string.Empty;
        ShowAssign = true;

        await Task.CompletedTask;
    }

    protected void CloseAssign()
    {
        ShowAssign = false;
    }

    protected async Task AssignEngineerAsync()
    {
        var user = AssignableUsers.FirstOrDefault(x => x.Email.Equals(SelectedUserEmail, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            SetError("Debe seleccionar un usuario.");
            return;
        }

        AssignRequest.AssignedTo = user.DisplayName;
        AssignRequest.AssignedEmail = user.Email;
        AssignRequest.AssignedBy = UserEmail;
        AssignRequest.RequiredDate = RequiredDateText;

        var result = await AppRegistrationService.AssignEngineerAsync(AssignRequest);

        if (result.Success)
        {
            SetOk(result.Message);
            ShowAssign = false;
        }
        else
        {
            SetError(result.ErrorMessage);
        }
    }

    protected async Task CopyClientIdAsync()
    {
        SetOk("ClientId copiado. Copia real al portapapeles se agrega en el siguiente sprint con JS interop.");
        await Task.CompletedTask;
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
        => value.HasValue ? value.Value.ToString("dd/MM/yyyy HH:mm") : "-";

    protected static string RiskCss(string risk)
        => risk switch
        {
            "Critico" => "critical",
            "Alto" => "high",
            "Medio" => "medium",
            "Bajo" => "low",
            "Saludable" => "healthy",
            _ => "healthy"
        };

    protected static string DaysCss(AppRegistrationListItem item)
        => item.DaysToExpire <= 15 ? "days-danger" : item.DaysToExpire <= 30 ? "days-warning" : "days-ok";

    protected static string HealthCss(int value)
        => value < 60 ? "bad" : value < 90 ? "warn" : "ok";

    private void SetOk(string message)
    {
        Message = message;
        MessageCss = "appreg-message ok";
    }

    private void SetError(string message)
    {
        Message = message;
        MessageCss = "appreg-message error";
    }
}
