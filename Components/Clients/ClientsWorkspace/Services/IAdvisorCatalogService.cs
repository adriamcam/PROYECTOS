using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services;

public interface IAdvisorCatalogService
{
    Task<IReadOnlyList<AdvisorCatalogItem>> GetAllAsync();
    Task<AdvisorCatalogSaveResult> CreateAsync(AdvisorCatalogItem item);
    Task<AdvisorCatalogSaveResult> UpdateAsync(AdvisorCatalogItem item);
    Task<AdvisorCatalogImportPreview> PreviewImportAsync(IReadOnlyCollection<AdvisorCatalogImportRow> rows);
    Task<AdvisorCatalogImportResult> ImportAsync(IReadOnlyCollection<AdvisorCatalogImportRow> rows);
    Task<AdvisorCatalogDeleteResult> DeleteManyAsync(IReadOnlyCollection<int> ids);
}
