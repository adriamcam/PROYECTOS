using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

namespace ITQS.SupportOperationsCenter.Repositories.Interfaces;

public interface IGdapAdminLinksRepository
{
    Task<GdapAdminLinksDashboardModel> GetDashboardAsync();
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetCustomersAsync(GdapAdminLinksFilterModel filters);
    Task<GdapAdminLinksCustomerModel?> GetCustomerAsync(int id);
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetPendingEmailsAsync();
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetExpiringSoonAsync();
    Task UpdateCustomerAsync(GdapAdminLinksSaveCustomerRequest request);
    Task SetCustomerActiveAsync(int id, bool isActive, string updatedBy, string reason);
}
