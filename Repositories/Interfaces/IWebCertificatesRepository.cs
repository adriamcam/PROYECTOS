using ITQS.SupportOperationsCenter.Models.Administration.WebCertificates;

namespace ITQS.SupportOperationsCenter.Repositories.Interfaces;

public interface IWebCertificatesRepository
{
    Task<WebCertificateDashboardModel> GetDashboardAsync();
    Task<IReadOnlyList<WebCertificateInventoryModel>> GetCertificatesAsync();
}