using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IGdapAdminLinksService
{
    Task<GdapAdminLinksDashboardModel> GetDashboardAsync();
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetCustomersAsync(GdapAdminLinksFilterModel filters);
    Task<GdapAdminLinksCustomerModel?> GetCustomerAsync(int id);
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetPendingEmailsAsync();
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetExpiringSoonAsync();
    Task<GdapAdminLinksActionResult> UpdateCustomerAsync(GdapAdminLinksSaveCustomerRequest request);
    Task<GdapAdminLinksActionResult> DisableCustomerAsync(int id, string updatedBy, string reason);
    Task<GdapAdminLinksActionResult> EnableCustomerAsync(int id, string updatedBy);
    Task<GdapAdminLinksActionResult> ExecuteAutomationAsync(int id, string requestedBy);
}
