using ITQS.SupportOperationsCenter.Models.Administration.WebCertificates;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class WebCertificatesService : IWebCertificatesService
{
    private readonly IWebCertificatesRepository _repository;

    public WebCertificatesService(IWebCertificatesRepository repository)
    {
        _repository = repository;
    }

    public Task<WebCertificateDashboardModel> GetDashboardAsync()
        => _repository.GetDashboardAsync();

    public Task<IReadOnlyList<WebCertificateInventoryModel>> GetCertificatesAsync()
        => _repository.GetCertificatesAsync();
}