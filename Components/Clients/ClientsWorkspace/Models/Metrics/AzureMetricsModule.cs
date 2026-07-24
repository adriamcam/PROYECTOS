namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Metrics;

public sealed class AzureMetricsModule
{
    public string CustomerName { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public Guid SubscriptionId { get; set; }

    public string SubscriptionName { get; set; } = string.Empty;

    public string ModuleKey { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int ResourceCount { get; set; }

    public bool IsEnabled { get; set; }

    public int DisplayOrder { get; set; }

    public string Icon => ModuleKey switch
    {
        "virtual-machines" => "🖥️",
        "storage-accounts" => "💾",
        "app-services" => "🌐",
        "sql-databases" => "🗄️",
        "sql-managed-instances" => "🏢",
        "postgresql-flexible-servers" => "🐘",
        "cosmos-db" => "🌌",
        "api-management" => "🔌",
        "data-factories" => "🏭",
        "container-registries" => "📦",
        "application-gateways" => "🚪",
        "redis-enterprise" => "🚀",
        "fabric-capacities" => "📊",
        "recovery-services" => "🛡️",
        _ => "☁️"
    };

    public string Description => ModuleKey switch
    {
        "virtual-machines" =>
            "CPU, memoria, disponibilidad, discos y recomendaciones.",

        "storage-accounts" =>
            "Capacidad, transacciones, tráfico, disponibilidad y latencia.",

        "app-services" =>
            "Solicitudes, errores, tiempo de respuesta, CPU y memoria.",

        "sql-databases" =>
            "CPU, DTU, almacenamiento, sesiones y rendimiento.",

        "sql-managed-instances" =>
            "Capacidad, almacenamiento y rendimiento de instancias administradas.",

        "postgresql-flexible-servers" =>
            "CPU, memoria, almacenamiento, conexiones y rendimiento.",

        "cosmos-db" =>
            "Solicitudes, RU consumidas, disponibilidad y latencia.",

        "api-management" =>
            "Solicitudes API, errores, capacidad y tiempo de respuesta.",

        "data-factories" =>
            "Ejecuciones, errores, duración y estado de pipelines.",

        "container-registries" =>
            "Capacidad, operaciones, disponibilidad y uso del registro.",

        "application-gateways" =>
            "Tráfico, conexiones, latencia, respuestas y estado del gateway.",

        "redis-enterprise" =>
            "CPU, memoria, conexiones, operaciones y latencia.",

        "fabric-capacities" =>
            "Capacidad, utilización, rendimiento y estado de Microsoft Fabric.",

        "recovery-services" =>
            "Estado de respaldos, duración, cumplimiento y recuperación.",

        _ =>
            "Métricas de rendimiento y disponibilidad del recurso."
    };

    public string StatusText =>
        ResourceCount > 0
            ? ResourceCount == 1
                ? "1 recurso"
                : $"{ResourceCount} recursos"
            : "Sin recursos";
}
