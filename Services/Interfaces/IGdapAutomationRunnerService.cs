using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IGdapAutomationRunnerService
{
    Task<GdapAdminLinksAutomationResult> StartRunbookForCustomerAsync(GdapAdminLinksAutomationRequest request);
}
