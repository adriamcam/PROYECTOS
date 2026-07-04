using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IGdapAdminLinksService
{
    Task<GdapAdminLinksDashboardModel> GetDashboardAsync();
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetCustomersAsync(GdapAdminLinksFilterModel filters);
    Task<GdapAdminLinksCustomerModel?> GetCustomerAsync(int id);
    Task<IReadOnlyList<GdapAdminLinksAuditEventModel>> GetAuditEventsAsync(int? customerId = null);
    Task<IReadOnlyList<GdapAdminLinksReportModel>> GetReportByPartnerAsync();
    Task<GdapAdminLinksExportResult> ExportCustomersCsvAsync(GdapAdminLinksFilterModel filters);
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetPendingEmailsAsync();
    Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetExpiringSoonAsync();
    Task<GdapAdminLinksActionResult> SendExpirationReminderEmailsAsync(int daysToExpire, int templateId, string sentBy);
    Task<GdapAdminLinksActionResult> UpdateCustomerAsync(GdapAdminLinksSaveCustomerRequest request);
    Task<GdapAdminLinksActionResult> DisableCustomerAsync(int id, string updatedBy, string reason);
    Task<GdapAdminLinksActionResult> EnableCustomerAsync(int id, string updatedBy);
    Task<GdapAdminLinksActionResult> ExecuteAutomationAsync(int id, string requestedBy);
    Task<GdapAdminLinksActionResult> SyncCustomerAsync(int id, string requestedBy);
    Task<IReadOnlyList<GdapMailTemplateModel>> GetMailTemplatesAsync();
    Task<GdapMailPreviewModel> PreviewEmailAsync(int customerId, int templateId);
    Task<GdapAdminLinksActionResult> SendEmailAsync(GdapMailSendRequest request);
    Task<GdapAdminLinksActionResult> SaveMailTemplateAsync(GdapMailTemplateModel template, string updatedBy);
    Task<GdapAdminLinksActionResult> UpdateCrmContactAsync(string customerTenantId, string primaryContact, string primaryContactEmail);
    Task RegisterMailSentAsync(int customerId, string sentBy, string sentTo);

Task<GdapAdminLinksActionResult> SetGdapAutomationStatusAsync(int id, bool enabled, string updatedBy, string reason);
}






