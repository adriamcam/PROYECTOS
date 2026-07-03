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
    Task RegisterHistoryAsync(int id, string eventType, string description, string executedBy, string? approvalUrl = null);
    Task MarkAutomationStartedAsync(GdapAdminLinksAutomationRequest request, string jobId);
    Task MarkAutomationFinishedAsync(GdapAdminLinksAutomationRequest request, GdapAdminLinksAutomationResult result);
}
