using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class GdapAdminLinksService : IGdapAdminLinksService
{
    private readonly IGdapAdminLinksRepository _repository;
    private readonly ILogger<GdapAdminLinksService> _logger;

    public GdapAdminLinksService(
        IGdapAdminLinksRepository repository,
        ILogger<GdapAdminLinksService> logger)
    {
        _repository = repository;
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
            return new GdapAdminLinksActionResult { Success = true, Message = "Cliente reactivado correctamente." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivando cliente GDAP {Id}", id);
            return new GdapAdminLinksActionResult { Success = false, ErrorMessage = ex.Message, Message = "No fue posible reactivar el cliente." };
        }
    }
}
