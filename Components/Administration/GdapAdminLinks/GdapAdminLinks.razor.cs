using System.Security.Claims;
using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ITQS.SupportOperationsCenter.Components.Administration.GdapAdminLinks;

public partial class GdapAdminLinks : ComponentBase
{
    [Inject] private IGdapAdminLinksService GdapService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    protected bool IsLoading { get; set; } = true;
    protected string UserEmail { get; set; } = string.Empty;
    protected string Message { get; set; } = string.Empty;
    protected string MessageCss { get; set; } = "gdap-message ok";
    protected string ActiveTab { get; set; } = "Dashboard";

    protected GdapAdminLinksDashboardModel Dashboard { get; set; } = new();
    protected GdapAdminLinksFilterModel Filters { get; set; } = new();

    protected List<GdapAdminLinksCustomerModel> Items { get; set; } = new();
    protected List<GdapAdminLinksCustomerModel> PendingEmailItems { get; set; } = new();

    protected int MailPageNumber { get; set; } = 1;
    protected int MailPageSize { get; set; } = 10;

    protected int MailTotalPages =>
        PendingEmailItems.Count <= 0 ? 1 : (int)Math.Ceiling(PendingEmailItems.Count / (double)MailPageSize);

    protected IReadOnlyList<GdapAdminLinksCustomerModel> PagedPendingEmailItems =>
        PendingEmailItems
            .Skip((MailPageNumber - 1) * MailPageSize)
            .Take(MailPageSize)
            .ToList();
    protected List<GdapAdminLinksCustomerModel> ExpiringItems { get; set; } = new();
    protected List<GdapAdminLinksReportModel> PartnerReports { get; set; } = new();
    protected List<GdapAdminLinksAuditEventModel> AuditEvents { get; set; } = new();
    protected List<GdapNotificationLogModel> NotificationLogs { get; set; } = new();
    protected string NotificationSortColumn { get; set; } = "SentAt";
    protected bool NotificationSortAscending { get; set; }

    protected List<GdapNotificationLogModel> SortedNotificationLogs =>
        ApplyNotificationSort(NotificationLogs).ToList();

    protected async Task SortNotificationsAsync(string column)
    {
        if (NotificationSortColumn == column)
            NotificationSortAscending = !NotificationSortAscending;
        else
        {
            NotificationSortColumn = column;
            NotificationSortAscending = column is "CustomerName" or "PartnerTenant" or "NotificationCase" or "NotificationStage";
        }

        await Task.CompletedTask;
    }

    protected string NotificationSortIcon(string column)
    {
        if (NotificationSortColumn != column)
            return "↕";

        return NotificationSortAscending ? "↑" : "↓";
    }

    private IEnumerable<GdapNotificationLogModel> ApplyNotificationSort(IEnumerable<GdapNotificationLogModel> source)
    {
        Func<GdapNotificationLogModel, object?> selector = NotificationSortColumn switch
        {
            "CustomerName" => x => x.CustomerName,
            "PartnerTenant" => x => x.PartnerTenant,
            "NotificationCase" => x => x.NotificationCase,
            "NotificationStage" => x => x.NotificationStage,
            "DaysToExpire" => x => x.DaysToExpire ?? int.MaxValue,
            "ActiveEndDate" => x => x.ActiveEndDate ?? DateTime.MaxValue,
            "SentTo" => x => x.SentTo,
            "Status" => x => x.Status,
            "SentAt" => x => x.SentAt ?? DateTime.MinValue,
            _ => x => x.SentAt ?? DateTime.MinValue
        };

        return NotificationSortAscending
            ? source.OrderBy(selector).ThenByDescending(x => x.Id)
            : source.OrderByDescending(selector).ThenByDescending(x => x.Id);
    }
    protected List<GdapAdminLinksAuditEventModel> NotificationEvents =>
        AuditEvents
            .Where(IsNotificationEvent)
            .OrderByDescending(x => x.EventDate)
            .Take(300)
            .ToList();

    private static bool IsNotificationEvent(GdapAdminLinksAuditEventModel item)
    {
        var text = $"{item.EventType} {item.Description}".ToLowerInvariant();

        return text.Contains("correo")
            || text.Contains("email")
            || text.Contains("mail")
            || text.Contains("notificacion")
            || text.Contains("notificación")
            || text.Contains("approval");
    }

    protected bool ShowDetail { get; set; }
    protected GdapAdminLinksCustomerModel? SelectedDetail { get; set; }

    protected bool ShowEdit { get; set; }
    protected string EditCustomerName { get; set; } = string.Empty;
    protected GdapAdminLinksSaveCustomerRequest EditRequest { get; set; } = new();

    protected bool ShowAutomationConfirm { get; set; }
    protected bool IsAutomationRunning { get; set; }
    protected GdapAdminLinksCustomerModel? AutomationCustomer { get; set; }

    protected bool ShowSyncConfirm { get; set; }
    protected bool IsSyncRunning { get; set; }
    protected GdapAdminLinksCustomerModel? SyncCustomer { get; set; }

    protected bool ShowDisableGdapConfirm { get; set; }
    protected bool IsDisableGdapSaving { get; set; }
    protected GdapAdminLinksCustomerModel? DisableGdapCustomer { get; set; }
    protected string DisableGdapReason { get; set; } = string.Empty;

    protected bool ShowMailPreview { get; set; }
    protected bool IsSendingMail { get; set; }
    protected bool ShowInfoPopup { get; set; }
    protected bool ShowEditCrmContactPopup { get; set; }
    protected GdapAdminLinksCustomerModel? EditCrmContactCustomer { get; set; }
    protected string EditCrmContactName { get; set; } = string.Empty;
    protected string EditCrmContactEmail { get; set; } = string.Empty;
    protected int SelectedTemplateId { get; set; }
    protected GdapMailPreviewModel? MailPreview { get; set; }
    protected List<GdapMailTemplateModel> MailTemplates { get; set; } = new();
    protected GdapMailTemplateModel TemplateEditor { get; set; } = new();
    protected bool ShowTemplateEditor { get; set; }

    protected int PageNumber { get; set; } = 1;
    protected int PageSize { get; set; } = 10;

    protected List<GdapAdminLinksCustomerModel> FilteredItems => Items.ToList();

    protected int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredItems.Count / (double)PageSize));

    protected List<GdapAdminLinksCustomerModel> PagedItems =>
        FilteredItems.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    protected int FirstItemNumber => FilteredItems.Count == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    protected int LastItemNumber => Math.Min(PageNumber * PageSize, FilteredItems.Count);

    protected int HealthPercent => (Dashboard.TotalCustomers - Dashboard.DisabledCustomers) <= 0
        ? 0
        : (int)Math.Round((Dashboard.ActiveGdap / (double)(Dashboard.TotalCustomers - Dashboard.DisabledCustomers)) * 100, 0);

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

    protected List<string> PartnerOptions { get; } =
[
    "Costa Rica",
    "Guatemala",
    "Panama"
];

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
            await LoadTemplatesAsync();
            await LoadReportsAsync();
            await LoadAuditAsync();
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
        else if (tab == "Templates")
            await LoadTemplatesAsync();
        else if (tab == "Notifications")
            await LoadAuditAsync();
        else if (tab == "Reports")
            await LoadReportsAsync();
        else if (tab == "Audit")
            await LoadAuditAsync();
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

    protected async Task LoadTemplatesAsync()
    {
        MailTemplates = (await GdapService.GetMailTemplatesAsync()).ToList();
        if (SelectedTemplateId <= 0)
            SelectedTemplateId = MailTemplates.FirstOrDefault(x => x.IsDefault)?.Id ?? MailTemplates.FirstOrDefault()?.Id ?? 0;
    }


    protected async Task LoadReportsAsync()
    {
        PartnerReports = (await GdapService.GetReportByPartnerAsync()).ToList();
    }

    protected async Task LoadAuditAsync()
    {
        AuditEvents = (await GdapService.GetAuditEventsAsync()).ToList();
    }
    protected async Task LoadNotificationLogsAsync()
    {
        NotificationLogs = (await GdapService.GetNotificationLogsAsync()).ToList();
    }

    protected async Task ExportCustomersCsvAsync()
    {
        var export = await GdapService.ExportCustomersCsvAsync(Filters);
        await JsRuntime.InvokeVoidAsync("eval", $"const a=document.createElement('a');a.href='data:{export.ContentType};base64,{export.Base64Content}';a.download='{export.FileName}';a.click();");
        SetOk($"Exportación generada: {export.TotalRows} registros.");
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

    protected async Task DisableGdapAutomationAsync(GdapAdminLinksCustomerModel item)
    {
        var result = await GdapService.SetGdapAutomationStatusAsync(
            item.Id,
            false,
            UserEmail,
            "Cliente sin servicios activos con ITQS.");

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

    protected async Task EnableGdapAutomationAsync(GdapAdminLinksCustomerModel item)
    {
        var result = await GdapService.SetGdapAutomationStatusAsync(
            item.Id,
            true,
            UserEmail,
            string.Empty);

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
    protected async Task ToggleGdapAutomationAsync(GdapAdminLinksCustomerModel item)
    {
        if (item.EnableGDAPAutomation)
        {
            DisableGdapCustomer = item;
            DisableGdapReason = string.Empty;
            ShowDisableGdapConfirm = true;
            return;
        }

        var result = await GdapService.SetGdapAutomationStatusAsync(
            item.Id,
            true,
            UserEmail,
            string.Empty);

        if (result.Success)
        {
            SetOk("Cliente habilitado para GDAP.");
            await RefreshAsync();
        }
        else
        {
            SetError(string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.Message : result.ErrorMessage);
        }
    }

    protected void CloseDisableGdapConfirm()
    {
        if (IsDisableGdapSaving)
            return;

        ShowDisableGdapConfirm = false;
        DisableGdapCustomer = null;
        DisableGdapReason = string.Empty;
    }

    protected async Task ConfirmDisableGdapAsync()
    {
        if (DisableGdapCustomer is null)
        {
            SetError("Debe seleccionar un cliente.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DisableGdapReason))
        {
            SetError("Debe indicar una justificación para deshabilitar GDAP.");
            return;
        }

        IsDisableGdapSaving = true;

        try
        {
            var result = await GdapService.SetGdapAutomationStatusAsync(
                DisableGdapCustomer.Id,
                false,
                UserEmail,
                DisableGdapReason.Trim());

            if (result.Success)
            {
                SetOk("Cliente deshabilitado para GDAP.");
                ShowDisableGdapConfirm = false;
                DisableGdapCustomer = null;
                DisableGdapReason = string.Empty;
                await RefreshAsync();
            }
            else
            {
                SetError(string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.Message : result.ErrorMessage);
            }
        }
        finally
        {
            IsDisableGdapSaving = false;
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


    protected async Task SyncCustomerAsync(GdapAdminLinksCustomerModel item)
    {
        var result = await GdapService.SyncCustomerAsync(item.Id, UserEmail);

        if (result.Success)
        {
            SetOk(result.Message);
            await RefreshAsync();
        }
        else
        {
            SetError(string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.Message : result.ErrorMessage);
        }
    }
    protected void OpenSyncConfirm(GdapAdminLinksCustomerModel item)
    {
        SyncCustomer = item;
        ShowSyncConfirm = true;
    }

    protected void CloseSyncConfirm()
    {
        if (IsSyncRunning)
            return;

        ShowSyncConfirm = false;
        SyncCustomer = null;
    }

    protected async Task ExecuteSyncCustomerAsync()
    {
        if (SyncCustomer is null)
        {
            SetError("Debe seleccionar un cliente.");
            return;
        }

        IsSyncRunning = true;

        try
        {
            var result = await GdapService.SyncCustomerAsync(SyncCustomer.Id, UserEmail);

            if (result.Success)
            {
                SetOk(result.Message);
                ShowSyncConfirm = false;
                SyncCustomer = null;
                await RefreshAsync();
            }
            else
            {
                SetError(string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.Message : result.ErrorMessage);
            }
        }
        finally
        {
            IsSyncRunning = false;
        }
    }
    protected void OpenAutomationConfirm(GdapAdminLinksCustomerModel item)
    {
        if (!item.IsActive)
        {
            SetError("El cliente está desactivado. Reactívelo antes de ejecutar la Automation.");
            return;
        }

        AutomationCustomer = item;
        ShowAutomationConfirm = true;
    }

    protected void CloseAutomationConfirm()
    {
        if (IsAutomationRunning)
            return;

        ShowAutomationConfirm = false;
        AutomationCustomer = null;
    }

    protected async Task ExecuteAutomationAsync()
    {
        if (AutomationCustomer is null)
        {
            SetError("Debe seleccionar un cliente.");
            return;
        }

        IsAutomationRunning = true;

        try
        {
            var result = await GdapService.ExecuteAutomationAsync(AutomationCustomer.Id, UserEmail);

            if (result.Success)
            {
                SetOk(result.Message);
                ShowAutomationConfirm = false;
                AutomationCustomer = null;
                await RefreshAsync();
            }
            else
            {
                SetError(string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.Message : result.ErrorMessage);
            }
        }
        finally
        {
            IsAutomationRunning = false;
        }
    }

    protected async Task CopyApprovalUrlAsync(GdapAdminLinksCustomerModel item)
    {
        if (string.IsNullOrWhiteSpace(item.ApprovalPendingLink))
        {
            SetError("El cliente no tiene Approval URL generado.");
            return;
        }

        await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", item.ApprovalPendingLink);
        SetOk("Approval URL copiado al portapapeles.");
    }



    protected async Task OpenMailPreviewAsync(GdapAdminLinksCustomerModel item)
    {
        try
        {
            if (SelectedTemplateId <= 0)
                await LoadTemplatesAsync();
            await LoadReportsAsync();
            await LoadAuditAsync();

            MailPreview = await GdapService.PreviewEmailAsync(item.Id, SelectedTemplateId);
            ShowMailPreview = true;
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    protected void CloseMailPreview()
    {
        ShowMailPreview = false;
        MailPreview = null;
    }

    protected void OpenMailTo()
    {
        if (MailPreview is null || string.IsNullOrWhiteSpace(MailPreview.MailToUrl))
        {
            SetError("No hay vista previa de correo disponible.");
            return;
        }

        NavigationManager.NavigateTo(MailPreview.MailToUrl, forceLoad: true);
    }

    protected async Task SendPreviewEmailAsync()
    {
        if (MailPreview is null)
        {
            SetError("No hay vista previa de correo disponible.");
            return;
        }

        IsSendingMail = true;

        try
        {
            var result = await GdapService.SendEmailAsync(new GdapMailSendRequest
            {
                CustomerId = MailPreview.CustomerId,
                TemplateId = MailPreview.TemplateId,
                SentBy = UserEmail
            });

            if (result.Success)
            {
                SetOk(result.Message);
                ShowMailPreview = false;
                await RefreshAsync();
            }
            else
            {
                SetError(result.ErrorMessage);
            }
        }
        finally
        {
            IsSendingMail = false;
        }
    }

    
    protected void OpenEditCrmContactPopup(GdapAdminLinksCustomerModel item)
    {
        EditCrmContactCustomer = item;
        EditCrmContactName = item.PrimaryContactName ?? string.Empty;
        EditCrmContactEmail = item.PrimaryEmail ?? string.Empty;
        ShowEditCrmContactPopup = true;
    }

    protected void CloseEditCrmContactPopup()
    {
        ShowEditCrmContactPopup = false;
        EditCrmContactCustomer = null;
        EditCrmContactName = string.Empty;
        EditCrmContactEmail = string.Empty;
    }

    protected async Task SaveCrmContactAsync()
    {
        if (EditCrmContactCustomer is null)
        {
            SetError("Debe seleccionar un cliente.");
            return;
        }

        var result = await GdapService.UpdateCrmContactAsync(
            EditCrmContactCustomer.CustomerTenantId,
            EditCrmContactName,
            EditCrmContactEmail);

        if (result.Success)
        {
            SetOk(result.Message);
            CloseEditCrmContactPopup();
            await RefreshAsync();
        }
        else
        {
            SetError(string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.Message : result.ErrorMessage);
        }
    }
protected async Task SendMailDirectAsync(GdapAdminLinksCustomerModel item)
    {
        var to = item.PrimaryEmail ?? string.Empty;
        var subject = Uri.EscapeDataString("Renovación de Permisos GDAP – Acción Requerida");

        var body = Uri.EscapeDataString($@"Quiero compartirte una información importante respecto a la renovación de permisos de nuestra relación de partner, que nos permite continuar brindando soporte y administración de tu entorno de Microsoft de manera segura y eficiente.

Como partner, necesitamos ciertos roles para visualizar el tenant y ofrecerte asistencia siempre que lo requieras. Actualmente, los permisos para el tenant con la relación ITQS han caducado.

Para seguir gestionando tu entorno correctamente, es imprescindible que aceptes los permisos GDAP (Granular Delegated Admin Privileges) lo antes posible. Estos permisos nos permiten acceder únicamente a los recursos necesarios, de forma segura, controlada y bajo las políticas de Microsoft, garantizando la confidencialidad de tu información.

Hemos generado un enlace para renovar la relación de administración como tu proveedor de servicios Microsoft (te aseguramos que este acceso No es Global Admin). Te agradecemos tu apoyo para aceptar los permisos y evitar perder el acceso a los roles necesarios.

Para ello, sigue estos pasos: abre un navegador como Chrome o Edge, preferiblemente en modo incógnito y accede al enlace en esa ventana de incógnito (el link se debe abrir con un usuario Global Admin del tenant y aceptar los permisos).

{item.ApprovalPendingLink}

¿Por qué es importante este acceso?

- Sin GDAP, ITQS no podrá entrar a la consola de administración para resolver incidencias técnicas, configurar cuentas o aplicar correcciones de seguridad de forma inmediata.

- Toda la gestión administrativa recaerá en tu equipo interno de TI, ya que no tendremos acceso para ayudar en estas tareas.

- Se pueden generar retrasos en la resolución de incidentes: si sucede alguna caída o problema crítico, ITQS tendría que guiar a tu equipo por videollamada o esperar a que realicen los cambios, lo cual incrementa el tiempo de inactividad.

- El aprovisionamiento de licencias podría verse afectado, ya que no podríamos asignarlas o renovarlas automáticamente y los usuarios podrían quedarse sin servicio si no se gestiona manualmente por parte de tu equipo.

- En situaciones urgentes, la falta de acceso granular puede llevar a compartir credenciales de administrador global, lo que supone un riesgo para la seguridad, frente al acceso controlado y auditable que ofrece GDAP.");

        await GdapService.RegisterMailSentAsync(item.Id, UserEmail, to);

        var mailToUrl = $"mailto:{to}?subject={subject}&body={body}";
        NavigationManager.NavigateTo(mailToUrl, forceLoad: true);

        await Task.CompletedTask;
    }

    protected async Task SendExpirationReminderBatchAsync(int daysToExpire)
    {
        IsSendingMail = true;

        try
        {
            if (SelectedTemplateId <= 0)
                await LoadTemplatesAsync();

            var result = await GdapService.SendExpirationReminderEmailsAsync(daysToExpire, SelectedTemplateId, UserEmail);

            if (result.Success)
            {
                SetOk(result.Message);
                await RefreshAsync();
            }
            else
            {
                SetError(string.IsNullOrWhiteSpace(result.ErrorMessage) ? result.Message : result.ErrorMessage);
            }
        }
        finally
        {
            IsSendingMail = false;
        }
    }


    protected void EditTemplate(GdapMailTemplateModel template)
    {
        TemplateEditor = new GdapMailTemplateModel
        {
            Id = template.Id,
            TemplateKey = template.TemplateKey,
            Name = template.Name,
            Subject = template.Subject,
            HtmlBody = template.HtmlBody,
            IsDefault = template.IsDefault,
            IsActive = template.IsActive,
            CreatedBy = template.CreatedBy,
            UpdatedBy = UserEmail
        };
        ShowTemplateEditor = true;
    }

    protected void NewTemplate()
    {
        TemplateEditor = new GdapMailTemplateModel
        {
            TemplateKey = "GDAP_APPROVAL_RENEWAL",
            Name = "Renovación GDAP",
            Subject = "Renovación de permisos GDAP - {{CustomerName}}",
            HtmlBody = "<p>Estimado(a) {{PrimaryContactName}},</p><p>Favor aprobar la relación GDAP desde el siguiente enlace:</p><p><a href=\"{{ApprovalUrl}}\">{{ApprovalUrl}}</a></p>",
            IsActive = true,
            IsDefault = false,
            CreatedBy = UserEmail,
            UpdatedBy = UserEmail
        };
        ShowTemplateEditor = true;
    }

    protected void CloseTemplateEditor()
    {
        ShowTemplateEditor = false;
    }

    protected async Task SaveTemplateAsync()
    {
        var result = await GdapService.SaveMailTemplateAsync(TemplateEditor, UserEmail);

        if (result.Success)
        {
            SetOk(result.Message);
            ShowTemplateEditor = false;
            await LoadTemplatesAsync();
            await LoadReportsAsync();
            await LoadAuditAsync();
        }
        else
        {
            SetError(result.ErrorMessage);
        }
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


    protected static string NotificationCaseText(string value)
    {
        return value switch
        {
            "GDAP_ACTIVE_EXPIRING_WITH_PENDING_APPROVAL_LINK" => "Vence con approval link",
            "GDAP_ACTIVE_EXPIRING_NO_PENDING_APPROVAL" => "Vence sin approval link",
            "GDAP_APPROVAL_PENDING" => "Approval pendiente",
            "GDAP_WITHOUT_RELATIONSHIP" => "Sin relación GDAP",
            _ => string.IsNullOrWhiteSpace(value) ? "-" : value.Replace("_", " ")
        };
    }

    protected static string NotificationStageText(string value)
    {
        return value switch
        {
            "INTERNAL_NO_PENDING_APPROVAL" => "Validación interna",
            "CLIENT_NOTIFICATION_1" => "Notificación cliente #1",
            "CLIENT_NOTIFICATION_2" => "Notificación cliente #2",
            "CLIENT_NOTIFICATION_3" => "Notificación cliente #3",
            "SALES_VALIDATION" => "Validación ventas",
            _ => string.IsNullOrWhiteSpace(value) ? "-" : value.Replace("_", " ")
        };
    }

    protected static string NotificationStatusText(string value)
    {
        return value switch
        {
            "Sent" => "Enviado",
            "TestIgnored" => "Ignorado prueba",
            "Failed" => "Fallido",
            "Pending" => "Pendiente",
            _ => string.IsNullOrWhiteSpace(value) ? "-" : value
        };
    }

    protected static string NotificationStatusCss(string value)
    {
        var status = value?.ToLowerInvariant() ?? string.Empty;

        if (status.Contains("sent")) return "active";
        if (status.Contains("failed") || status.Contains("error")) return "danger";
        if (status.Contains("ignored")) return "neutral";
        if (status.Contains("pending")) return "pending";

        return "neutral";
    }
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



















