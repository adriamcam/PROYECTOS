using System.Security.Claims;
using ITQS.SupportOperationsCenter.Models.Administration.Customers;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

namespace ITQS.SupportOperationsCenter.Components.Administration.Customers;

public partial class CustomerAdmin : ComponentBase
{
    [Inject] private ICustomerAdminService CustomerAdminService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ICustomerConnectionRunbookService CustomerConnectionRunbookService { get; set; } = default!;

    protected bool IsLoading { get; set; } = true;
    protected bool CanAccess { get; set; }
    protected bool ShowEditor { get; set; }
    protected bool IsEditMode { get; set; }
    protected bool ShowDeleteCustomerModal { get; set; }

    protected CustomerAdminModel? CustomerToDelete { get; set; }

    protected string UserEmail { get; set; } = string.Empty;
    protected string SearchText { get; set; } = string.Empty;
    protected string SelectedStatus { get; set; } = "All";
    protected string Message { get; set; } = string.Empty;
    protected string MessageCss { get; set; } = "customer-message ok";
    protected string TenantIdText { get; set; } = string.Empty;
    protected string OriginalTenantIdText { get; set; } = string.Empty;

    protected CustomerAdminDashboardModel Dashboard { get; set; } = new();
    protected List<CustomerAdminModel> Customers { get; set; } = new();
    protected CustomerAdminSaveRequestModel Editor { get; set; } = new();

    protected int PageNumber { get; set; } = 1;
    protected int PageSize { get; set; } = 10;

    protected List<CustomerAdminModel> FilteredCustomers => Customers.ToList();

    protected int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCustomers.Count / (double)PageSize));

    protected List<CustomerAdminModel> PagedCustomers =>
        FilteredCustomers
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

    protected int FirstItemNumber => FilteredCustomers.Count == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

    protected int LastItemNumber => Math.Min(PageNumber * PageSize, FilteredCustomers.Count);

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

    protected bool ShowConnectionTest { get; set; }
    protected bool IsRunningConnectionTest { get; set; }

    protected string ConnectionCustomerName { get; set; } = string.Empty;
    protected string ConnectionTenantId { get; set; } = string.Empty;
    protected string ConnectionClientId { get; set; } = string.Empty;
    protected string ConnectionSecretName { get; set; } = string.Empty;
    protected string ConnectionStatus { get; set; } = string.Empty;
    protected string ConnectionSource { get; set; } = string.Empty;
    protected string ConnectionResult { get; set; } = "Listo para ejecutar validación.";

    protected string ConnectionJobId { get; set; } = string.Empty;
    protected string ConnectionJobStatus { get; set; } = "Ready";
    protected int ConnectionAttempt { get; set; }
    protected int ConnectionMaxAttempts { get; set; } = 60;

    protected string ConnectionStatusCss =>
        ConnectionJobStatus.ToUpperInvariant() switch
        {
            "COMPLETED" => "success",
            "FAILED" => "failed",
            "STOPPED" => "failed",
            "SUSPENDED" => "failed",
            "RUNNING" => "running",
            "ACTIVATING" => "pending",
            "NEW" => "pending",
            "STARTED" => "pending",
            "READY" => "idle",
            _ => "pending"
        };

    protected string ConnectionStatusIcon =>
        ConnectionStatusCss switch
        {
            "success" => "✅",
            "failed" => "❌",
            "running" => "🔄",
            "pending" => "⏳",
            _ => "🔌"
        };

    protected string ConnectionStatusTitle =>
        ConnectionStatusCss switch
        {
            "success" => "Validación finalizada correctamente",
            "failed" => "Validación finalizada con error",
            "running" => "Validación en ejecución",
            "pending" => "Azure Automation preparando ejecución",
            _ => "Listo para validar conexión"
        };

    protected string ConnectionStatusSubtitle =>
        ConnectionStatusCss switch
        {
            "success" => "El runbook terminó correctamente. Revisa el log técnico para el detalle.",
            "failed" => "El runbook terminó con error o fue detenido. Revisa el log técnico.",
            "running" => "El runbook está ejecutando las validaciones del cliente.",
            "pending" => "La solicitud fue enviada y Azure Automation está preparando el job.",
            _ => "Presiona Ejecutar prueba para iniciar el runbook."
        };

    protected int ConnectionProgressPercent =>
        ConnectionStatusCss switch
        {
            "success" => 100,
            "failed" => 100,
            "running" => Math.Clamp(35 + (ConnectionAttempt * 2), 35, 85),
            "pending" => Math.Clamp(10 + ConnectionAttempt, 10, 35),
            _ => 0
        };

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        UserEmail = user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.Identity?.Name
            ?? string.Empty;

        CanAccess = await CustomerAdminService.CanAccessAsync(UserEmail);

        if (CanAccess)
        {
            await RefreshAsync();
        }

        IsLoading = false;
    }

    protected async Task RefreshAsync()
    {
        IsLoading = true;

        try
        {
            Dashboard = await CustomerAdminService.GetDashboardAsync();
            await LoadCustomersAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task LoadCustomersAsync()
    {
        Customers = (await CustomerAdminService.GetCustomersAsync(SearchText, SelectedStatus)).ToList();
        EnsureValidPage();
    }

    protected async Task ApplyFiltersAsync()
    {
        PageNumber = 1;
        await LoadCustomersAsync();
    }

    protected async Task SearchKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await ApplyFiltersAsync();
        }
    }

    protected void OpenNewCustomer()
    {
        Message = string.Empty;
        IsEditMode = false;
        TenantIdText = string.Empty;
        OriginalTenantIdText = string.Empty;

        Editor = new CustomerAdminSaveRequestModel
        {
            IsActive = true,
            Source = "SupportCloud"
        };

        ShowEditor = true;
    }

    protected void OpenEditCustomer(CustomerAdminModel customer)
    {
        Message = string.Empty;
        IsEditMode = true;
        TenantIdText = customer.TenantId.ToString();
        OriginalTenantIdText = customer.TenantId.ToString();

        Editor = new CustomerAdminSaveRequestModel
        {
            TenantId = customer.TenantId,
            CustomerName = customer.CustomerName,
            CustomerNamePortal = customer.CustomerNamePortal,
            ClientId = customer.ClientId,
            SecretName = customer.SecretName,
            IsActive = customer.IsActive,
            Source = string.IsNullOrWhiteSpace(customer.Source) ? "SupportCloud" : customer.Source,
            Notes = customer.Notes
        };

        ShowEditor = true;
    }

    protected void CloseEditor()
    {
        ShowEditor = false;
        OriginalTenantIdText = string.Empty;
    }

    protected async Task SaveAsync()
    {
        try
        {
            if (!Guid.TryParse(TenantIdText, out var tenantId))
                throw new InvalidOperationException("TenantId inválido.");

            Editor.TenantId = tenantId;
            Editor.UpdatedBy = UserEmail;

            Guid? originalTenantId = null;

            if (IsEditMode && Guid.TryParse(OriginalTenantIdText, out var parsedOriginalTenantId))
            {
                originalTenantId = parsedOriginalTenantId;
            }

            await CustomerAdminService.SaveCustomerAsync(Editor, originalTenantId);

            SetOk("Cliente guardado correctamente.");

            ShowEditor = false;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    protected void OpenDeleteCustomer(CustomerAdminModel customer)
    {
        Message = string.Empty;
        CustomerToDelete = customer;
        ShowDeleteCustomerModal = true;
    }

    protected void CloseDeleteCustomer()
    {
        ShowDeleteCustomerModal = false;
        CustomerToDelete = null;
    }

    protected async Task DeleteCustomerAsync()
    {
        if (CustomerToDelete is null)
            return;

        try
        {
            await CustomerAdminService.DeleteCustomerAsync(CustomerToDelete.TenantId, UserEmail);

            SetOk($"Cliente '{CustomerToDelete.CustomerName}' eliminado correctamente.");

            ShowDeleteCustomerModal = false;
            CustomerToDelete = null;

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    protected void OpenConnectionTest(CustomerAdminModel customer)
    {
        ConnectionCustomerName = customer.CustomerName;
        ConnectionTenantId = customer.TenantId.ToString();
        ConnectionClientId = customer.ClientId;
        ConnectionSecretName = customer.SecretName;
        ConnectionStatus = customer.IsActive ? "Activo" : "Inactivo";
        ConnectionSource = string.IsNullOrWhiteSpace(customer.Source) ? "-" : customer.Source;

        ConnectionJobId = string.Empty;
        ConnectionJobStatus = "Ready";
        ConnectionAttempt = 0;
        ConnectionMaxAttempts = 60;

        ConnectionResult =
            "Parámetros cargados automáticamente desde dbo.ITQS_Customers." +
            Environment.NewLine +
            Environment.NewLine +
            $"Cliente: {ConnectionCustomerName}" +
            Environment.NewLine +
            $"TenantId: {ConnectionTenantId}" +
            Environment.NewLine +
            $"ClientId: {ConnectionClientId}" +
            Environment.NewLine +
            $"SecretName: {ConnectionSecretName}" +
            Environment.NewLine +
            $"Estado: {ConnectionStatus}" +
            Environment.NewLine +
            Environment.NewLine +
            "Listo para ejecutar Azure Automation.";

        ShowConnectionTest = true;
    }

    protected void CloseConnectionTest()
    {
        ShowConnectionTest = false;
    }

    protected async Task RunConnectionTest()
    {
        if (IsRunningConnectionTest)
            return;

        IsRunningConnectionTest = true;
        ConnectionAttempt = 0;
        ConnectionJobStatus = "Starting";

        try
        {
            if (!Guid.TryParse(ConnectionTenantId, out var tenantId))
                throw new InvalidOperationException("TenantId inválido.");

            ConnectionResult =
                "Iniciando runbook en Azure Automation..." +
                Environment.NewLine +
                $"Runbook: ITQS-SOC-VALIDATE-CONNECTIONS-CLIENTES" +
                Environment.NewLine +
                $"Cliente: {ConnectionCustomerName}" +
                Environment.NewLine +
                $"TenantId: {ConnectionTenantId}";

            await InvokeAsync(StateHasChanged);

            var result = await CustomerConnectionRunbookService.StartValidationAsync(
                new CustomerConnectionRunbookRequest
                {
                    CustomerName = ConnectionCustomerName,
                    TenantId = tenantId,
                    ClientId = ConnectionClientId,
                    SecretName = ConnectionSecretName,
                    RequestedBy = UserEmail
                });

            ConnectionJobId = result.JobId;
            ConnectionJobStatus = result.Status;

            if (!result.Started)
            {
                ConnectionResult =
                    "Error iniciando runbook." +
                    Environment.NewLine +
                    Environment.NewLine +
                    $"Runbook: {result.RunbookName}" +
                    Environment.NewLine +
                    $"Estado: {result.Status}" +
                    Environment.NewLine +
                    $"Detalle: {result.ErrorMessage}";

                return;
            }

            ConnectionResult =
                "Runbook iniciado correctamente." +
                Environment.NewLine +
                Environment.NewLine +
                $"Runbook: {result.RunbookName}" +
                Environment.NewLine +
                $"JobId: {result.JobId}" +
                Environment.NewLine +
                "Estado: Started" +
                Environment.NewLine +
                Environment.NewLine +
                "Consultando estado del job...";

            await InvokeAsync(StateHasChanged);

            ConnectionMaxAttempts = 60;
            var delaySeconds = 3;

            for (var attempt = 1; attempt <= ConnectionMaxAttempts; attempt++)
            {
                ConnectionAttempt = attempt;

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

                var status = await CustomerConnectionRunbookService.GetJobStatusAsync(result.JobId);

                ConnectionJobStatus = string.IsNullOrWhiteSpace(status.Status)
                    ? "Unknown"
                    : status.Status;

                ConnectionResult =
                    "Monitoreo de ejecución del runbook" +
                    Environment.NewLine +
                    Environment.NewLine +
                    $"Cliente: {ConnectionCustomerName}" +
                    Environment.NewLine +
                    $"Runbook: {result.RunbookName}" +
                    Environment.NewLine +
                    $"JobId: {result.JobId}" +
                    Environment.NewLine +
                    $"Estado: {status.Status}" +
                    Environment.NewLine +
                    $"Detalle: {status.StatusDetails}" +
                    Environment.NewLine +
                    $"Intento: {attempt}/{ConnectionMaxAttempts}" +
                    Environment.NewLine +
                    $"Inicio: {(status.StartTime?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") ?? "-")}" +
                    Environment.NewLine +
                    $"Fin: {(status.EndTime?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") ?? "-")}" +
                    Environment.NewLine +
                    Environment.NewLine +
                    status.Message;

                if (!string.IsNullOrWhiteSpace(status.ErrorMessage))
                {
                    ConnectionResult +=
                        Environment.NewLine +
                        Environment.NewLine +
                        "Error:" +
                        Environment.NewLine +
                        status.ErrorMessage;
                }

                await InvokeAsync(StateHasChanged);

                if (status.IsFinal)
                {
                    ConnectionResult +=
                        Environment.NewLine +
                        Environment.NewLine +
                        "Validación finalizada.";

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            ConnectionJobStatus = "Failed";

            ConnectionResult =
                "Error ejecutando validación." +
                Environment.NewLine +
                ex.Message;
        }
        finally
        {
            IsRunningConnectionTest = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task GoToPageAsync(int page)
    {
        PageNumber = Math.Clamp(page, 1, TotalPages);
        await Task.CompletedTask;
    }

    protected async Task PreviousPageAsync()
    {
        if (PageNumber > 1)
        {
            PageNumber--;
        }

        await Task.CompletedTask;
    }

    protected async Task NextPageAsync()
    {
        if (PageNumber < TotalPages)
        {
            PageNumber++;
        }

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
        if (PageNumber > TotalPages)
        {
            PageNumber = TotalPages;
        }

        if (PageNumber < 1)
        {
            PageNumber = 1;
        }
    }

    protected static string DisplayEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value;

    protected static string FormatDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("dd/MM/yyyy HH:mm") : "-";

    private void SetOk(string message)
    {
        Message = message;
        MessageCss = "customer-message ok";
    }

    private void SetError(string message)
    {
        Message = message;
        MessageCss = "customer-message error";
    }
}
