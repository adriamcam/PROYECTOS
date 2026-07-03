using ITQS.SupportOperationsCenter.Models.Administration.Customers;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface ICustomerAdminService
{
    Task<bool> CanAccessAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<CustomerAdminDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerAdminModel>> GetCustomersAsync(string searchText, string status, CancellationToken cancellationToken = default);
    Task<CustomerAdminModel> SaveCustomerAsync(CustomerAdminSaveRequestModel request, Guid? originalTenantId = null, CancellationToken cancellationToken = default);
    Task DeleteCustomerAsync(Guid tenantId, string userEmail, CancellationToken cancellationToken = default);
}
