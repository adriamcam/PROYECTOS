using ITQS.SupportOperationsCenter.Models.Administration.Customers;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class CustomerAdminService : ICustomerAdminService
{
    private readonly ICustomerAdminRepository _repository;
    private readonly ILogger<CustomerAdminService> _logger;

    public CustomerAdminService(ICustomerAdminRepository repository, ILogger<CustomerAdminService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        try { return await _repository.CanAccessAsync(userEmail, cancellationToken); }
        catch (Exception ex) { _logger.LogError(ex, "Error validating Customer Admin access."); return false; }
    }

    public async Task<CustomerAdminDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        try { return await _repository.GetDashboardAsync(cancellationToken); }
        catch (Exception ex) { _logger.LogError(ex, "Error loading Customer Admin dashboard."); return new CustomerAdminDashboardModel(); }
    }

    public async Task<IReadOnlyList<CustomerAdminModel>> GetCustomersAsync(string searchText, string status, CancellationToken cancellationToken = default)
    {
        try { return await _repository.GetCustomersAsync(searchText, status, cancellationToken); }
        catch (Exception ex) { _logger.LogError(ex, "Error loading customers."); return Array.Empty<CustomerAdminModel>(); }
    }

    public async Task<CustomerAdminModel> SaveCustomerAsync(CustomerAdminSaveRequestModel request, Guid? originalTenantId = null, CancellationToken cancellationToken = default)
    {
        request.CustomerName = request.CustomerName?.Trim() ?? string.Empty;
        request.CustomerNamePortal = request.CustomerNamePortal?.Trim() ?? string.Empty;
        request.ClientId = request.ClientId?.Trim() ?? string.Empty;
        request.SecretName = request.SecretName?.Trim() ?? string.Empty;
        request.Notes = request.Notes?.Trim() ?? string.Empty;
        request.Source = string.IsNullOrWhiteSpace(request.Source) ? "SupportCloud" : request.Source.Trim();

        if (!request.TenantId.HasValue || request.TenantId == Guid.Empty)
            throw new InvalidOperationException("El TenantId es requerido.");

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new InvalidOperationException("El nombre del cliente es requerido.");

        if (string.IsNullOrWhiteSpace(request.CustomerNamePortal))
            request.CustomerNamePortal = request.CustomerName;

        if (string.IsNullOrWhiteSpace(request.ClientId))
            throw new InvalidOperationException("El ClientId es requerido.");

        if (string.IsNullOrWhiteSpace(request.SecretName))
            throw new InvalidOperationException("El SecretName es requerido.");

        return await _repository.SaveCustomerAsync(request, originalTenantId, cancellationToken);
    }

    public async Task DeleteCustomerAsync(Guid tenantId, string userEmail, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("TenantId inválido.");

        if (string.IsNullOrWhiteSpace(userEmail))
            throw new InvalidOperationException("No se pudo identificar el usuario que elimina el cliente.");

        await _repository.DeleteCustomerAsync(tenantId, userEmail.Trim(), cancellationToken);
    }
}
