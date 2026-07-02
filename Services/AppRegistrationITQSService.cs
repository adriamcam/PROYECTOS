using ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class AppRegistrationITQSService : IAppRegistrationITQSService
{
    private readonly IAppRegistrationITQSRepository _repository;
    private readonly IAppRegistrationNotificationService _notificationService;
    private readonly ILogger<AppRegistrationITQSService> _logger;

    public AppRegistrationITQSService(
        IAppRegistrationITQSRepository repository,
        IAppRegistrationNotificationService notificationService,
        ILogger<AppRegistrationITQSService> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public Task<AppRegistrationDashboardModel> GetDashboardAsync()
        => _repository.GetDashboardAsync();

    public Task<IReadOnlyList<AppRegistrationListItem>> GetListAsync(AppRegistrationFilterModel filters)
        => _repository.GetListAsync(filters);

    public Task<AppRegistrationDetailModel?> GetDetailAsync(long id)
        => _repository.GetDetailAsync(id);

    public Task<IReadOnlyList<AppRegistrationAssignableUserModel>> GetAssignableUsersAsync()
        => _repository.GetAssignableUsersAsync();

    public async Task<AppRegistrationAssignResult> AssignEngineerAsync(AppRegistrationAssignRequest request)
    {
        try
        {
            if (request.AppRegistrationId <= 0)
                throw new InvalidOperationException("Registro de App Registration inválido.");

            if (string.IsNullOrWhiteSpace(request.AssignedEmail))
                throw new InvalidOperationException("Debe seleccionar un usuario válido.");

            if (string.IsNullOrWhiteSpace(request.AssignedBy))
                throw new InvalidOperationException("No se pudo identificar el usuario que asigna la tarea.");

            var taskId = await _repository.CreateTaskAsync(request);

            var emailSent = false;

            try
            {
                await _notificationService.SendAssignmentEmailAsync(request, taskId);
                await _repository.MarkTaskEmailSentAsync(taskId);
                emailSent = true;
            }
           catch (Exception emailEx)
{
    _logger.LogError(emailEx, "La tarea {TaskId} fue creada, pero falló el envío del correo.", taskId);

    return new AppRegistrationAssignResult
    {
        Success = true,
        TaskId = taskId,
        EmailSent = false,
        Message = $"Tarea {taskId} asignada, pero falló el correo: {emailEx.Message}"
    };
}

            return new AppRegistrationAssignResult
            {
                Success = true,
                TaskId = taskId,
                EmailSent = emailSent,
                Message = emailSent
                    ? $"Tarea {taskId} asignada y correo enviado correctamente."
                    : $"Tarea {taskId} asignada, pero el correo no pudo enviarse."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asignando App Registration {AppRegistrationId}", request.AppRegistrationId);

            return new AppRegistrationAssignResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Message = "No fue posible asignar la tarea."
            };
        }
    }
}
