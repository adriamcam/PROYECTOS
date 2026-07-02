using ITQS.SupportOperationsCenter.Models.Administration.Customers;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface ICustomerConnectionRunbookService
{
    Task<CustomerConnectionRunbookResult> StartValidationAsync(
        CustomerConnectionRunbookRequest request,
        CancellationToken cancellationToken = default);
}
