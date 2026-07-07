using ITQS.SupportOperationsCenter.Models.Administration.WebCertificates;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IWebCertificatesService
{
    Task<WebCertificateDashboardModel> GetDashboardAsync();
    Task<IReadOnlyList<WebCertificateInventoryModel>> GetCertificatesAsync();
}