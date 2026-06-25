using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Repositories;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using ITQS.SupportOperationsCenter.Services;
using ITQS.SupportOperationsCenter.Services.Interfaces;

namespace ITQS.SupportOperationsCenter.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddItqsSocServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SqlSettings>(
            configuration.GetSection("SqlSettings"));

        services.Configure<KeyVaultSettings>(
            configuration.GetSection("KeyVaultSettings"));

        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();

        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IAlertService, AlertService>();

        services.AddScoped<IAssignedAlertsDashboardRepository, AssignedAlertsDashboardRepository>();
        services.AddScoped<IAssignedAlertsDashboardService, AssignedAlertsDashboardService>();

        return services;
    }
}