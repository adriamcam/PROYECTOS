using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

namespace ITQS.SupportOperationsCenter.Repositories.Interfaces;

public interface IGdapAdminLinksRepository
{
    Task<GdapAdminLinksDashboardModel> GetDashboardAsync();
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetCustomersAsync(GdapAdminLinksFilterModel filters);
    Task<GdapAdminLinksCustomerModel?> GetCustomerAsync(int id);
    Task UpdateCrmContactAsync(string customerTenantId, string primaryContact, string primaryContactEmail);
    Task<IReadOnlyList<GdapAdminLinksAuditEventModel>> GetAuditEventsAsync(int? customerId = null);
    Task<IReadOnlyList<GdapAdminLinksReportModel>> GetReportByPartnerAsync();
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetPendingEmailsAsync();
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetExpiringSoonAsync();
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetExpirationEmailQueueAsync(int daysToExpire);
    Task UpdateCustomerAsync(GdapAdminLinksSaveCustomerRequest request);
    Task SetCustomerActiveAsync(int id, bool isActive, string updatedBy, string reason);
    Task RegisterHistoryAsync(int id, string eventType, string description, string executedBy, string? approvalUrl = null);
    Task MarkAutomationStartedAsync(GdapAdminLinksAutomationRequest request, string jobId);
    Task MarkAutomationFinishedAsync(GdapAdminLinksAutomationRequest request, GdapAdminLinksAutomationResult result);
    Task<IReadOnlyList<GdapMailTemplateModel>> GetMailTemplatesAsync();
    Task<GdapMailTemplateModel?> GetMailTemplateAsync(int id);
    Task<GdapMailTemplateModel?> GetDefaultMailTemplateAsync();
    Task<int> SaveMailTemplateAsync(GdapMailTemplateModel template);
    Task MarkEmailSentAsync(int customerId, string sentBy);
    Task MarkEmailFailedAsync(int customerId, string sentBy, string errorMessage);

Task SetGdapAutomationStatusAsync(int id, bool enabled, string updatedBy, string reason);
}


