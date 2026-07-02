using ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IAppRegistrationITQSService
{
    Task<AppRegistrationDashboardModel> GetDashboardAsync();
    Task<IReadOnlyList<AppRegistrationListItem>> GetListAsync(AppRegistrationFilterModel filters);
    Task<AppRegistrationDetailModel?> GetDetailAsync(long id);
    Task<IReadOnlyList<AppRegistrationAssignableUserModel>> GetAssignableUsersAsync();
    Task<AppRegistrationAssignResult> AssignEngineerAsync(AppRegistrationAssignRequest request);
}
