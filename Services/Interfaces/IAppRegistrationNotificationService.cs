using ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;

namespace ITQS.SupportOperationsCenter.Services.Interfaces;

public interface IAppRegistrationNotificationService
{
    Task SendAssignmentEmailAsync(AppRegistrationAssignRequest request, long taskId);
}
