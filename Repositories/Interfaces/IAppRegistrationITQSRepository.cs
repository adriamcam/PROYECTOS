using ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

namespace ITQS.SupportOperationsCenter.Repositories.Interfaces;

public interface IAppRegistrationITQSRepository
{
    Task<AppRegistrationDashboardModel> GetDashboardAsync();
    Task<IReadOnlyList<AppRegistrationListItem>> GetListAsync(AppRegistrationFilterModel filters);
    Task<AppRegistrationDetailModel?> GetDetailAsync(long id);
    Task<IReadOnlyList<AppRegistrationAssignableUserModel>> GetAssignableUsersAsync();
    Task<long> CreateTaskAsync(AppRegistrationAssignRequest request);
    Task MarkTaskEmailSentAsync(long taskId);
}
