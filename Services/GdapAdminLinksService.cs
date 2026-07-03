using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class GdapAdminLinksService : IGdapAdminLinksService
{
    private readonly IGdapAdminLinksRepository _repository;
    private readonly IGdapAutomationRunnerService _automationRunner;
    private readonly GdapAutomationSettings _automationSettings;
    private readonly ILogger<GdapAdminLinksService> _logger;

    public GdapAdminLinksService(
        IGdapAdminLinksRepository repository,
        IGdapAutomationRunnerService automationRunner,
        IOptions<GdapAutomationSettings> automationOptions,
        ILogger<GdapAdminLinksService> logger)
    {
        _repository = repository;
        _automationRunner = automationRunner;
        _automationSettings = automationOptions.Value;
        _logger = logger;
    }

    public Task<GdapAdminLinksDashboardModel> GetDashboardAsync()
        => _repository.GetDashboardAsync();

    public Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetCustomersAsync(GdapAdminLinksFilterModel filters)
        => _repository.GetCustomersAsync(filters);

    public Task<GdapAdminLinksCustomerModel?> GetCustomerAsync(int id)
        => _repository.GetCustomerAsync(id);

    public Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetPendingEmailsAsync()
        => _repository.GetPendingEmailsAsync();

    public Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetExpiringSoonAsync()
        => _repository.GetExpiringSoonAsync();

    public async Task<GdapAdminLinksActionResult> UpdateCustomerAsync(GdapAdminLinksSaveCustomerRequest request)
    {
        try
        {
            if (request.Id <= 0)
                throw new InvalidOperationException("Cliente inválido.");

            if (!string.IsNullOrWhiteSpace(request.PrimaryEmail) && !request.PrimaryEmail.Contains('@'))
                throw new InvalidOperationException("El correo principal no tiene un formato válido.");

            await _repository.UpdateCustomerAsync(request);
            await _repository.RegisterHistoryAsync(request.Id, "Cliente actualizado", "Se actualizaron los datos de contacto/configuración del cliente.", request.UpdatedBy);

            return new GdapAdminLinksActionResult
            {
                Success = true,
                Message = "Cliente actualizado correctamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando cliente GDAP {Id}", request.Id);
            return new GdapAdminLinksActionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = "No fue posible actualizar el cliente."
            };
        }
    }

    public async Task<GdapAdminLinksActionResult> DisableCustomerAsync(int id, string updatedBy, string reason)
    {
        try
        {
            await _repository.SetCustomerActiveAsync(id, false, updatedBy, reason);
            await _repository.RegisterHistoryAsync(id, "Cliente desactivado", reason, updatedBy);
            return new GdapAdminLinksActionResult { Success = true, Message = "Cliente desactivado correctamente." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error desactivando cliente GDAP {Id}", id);
            return new GdapAdminLinksActionResult { Success = false, ErrorMessage = ex.Message, Message = "No fue posible desactivar el cliente." };
        }
    }

    public async Task<GdapAdminLinksActionResult> EnableCustomerAsync(int id, string updatedBy)
    {
        try
        {
            await _repository.SetCustomerActiveAsync(id, true, updatedBy, string.Empty);
            await _repository.RegisterHistoryAsync(id, "Cliente reactivado", "Cliente habilitado nuevamente para procesamiento GDAP.", updatedBy);
            return new GdapAdminLinksActionResult { Success = true, Message = "Cliente reactivado correctamente." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivando cliente GDAP {Id}", id);
            return new GdapAdminLinksActionResult { Success = false, ErrorMessage = ex.Message, Message = "No fue posible reactivar el cliente." };
        }
    }

    public async Task<GdapAdminLinksActionResult> ExecuteAutomationAsync(int id, string requestedBy)
    {
        try
        {
            var customer = await _repository.GetCustomerAsync(id);
            if (customer is null)
                throw new InvalidOperationException("No se encontró el cliente seleccionado.");

            if (!customer.IsActive)
                throw new InvalidOperationException("El cliente está desactivado. Reactívelo antes de ejecutar la Automation.");

            if (string.IsNullOrWhiteSpace(customer.CustomerTenantId))
                throw new InvalidOperationException("El cliente no tiene CustomerTenantId configurado.");

            if (string.IsNullOrWhiteSpace(customer.PartnerTenant))
                throw new InvalidOperationException("El cliente no tiene PartnerTenant configurado.");

            var request = new GdapAdminLinksAutomationRequest
            {
                CustomerId = customer.Id,
                PartnerTenant = customer.PartnerTenant,
                CustomerName = customer.CustomerName,
                CustomerTenantId = customer.CustomerTenantId,
                DaysThreshold = _automationSettings.DefaultDaysThreshold <= 0 ? 30 : _automationSettings.DefaultDaysThreshold,
                RequestedBy = requestedBy
            };

            await _repository.MarkAutomationStartedAsync(request, string.Empty);
            var result = await _automationRunner.StartRunbookForCustomerAsync(request);
            await _repository.MarkAutomationFinishedAsync(request, result);

            if (!result.Success)
            {
                return new GdapAdminLinksActionResult
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage,
                    Message = result.Message
                };
            }

            return new GdapAdminLinksActionResult
            {
                Success = true,
                Message = result.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando Automation GDAP para cliente {Id}", id);
            return new GdapAdminLinksActionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = "No fue posible ejecutar la Automation GDAP."
            };
        }
    }
}
