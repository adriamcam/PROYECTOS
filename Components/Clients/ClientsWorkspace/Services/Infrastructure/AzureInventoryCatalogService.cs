using System.Data;
using System.Text;
using Dapper;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Infrastructure;
using ITQS.SupportOperationsCenter.Data;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services.Infrastructure;

public sealed class AzureInventoryCatalogService
{
    private const string UnifiedServicesSource =
        "dbo.AzureServicesInventoryCurrent";

    private const string DynamicKeyPrefix =
        "azure-service-";

    private readonly ISqlConnectionFactory _connectionFactory;

    private static readonly IReadOnlyList<AzureInventoryModule> SpecializedCatalog =
    [
        new()
        {
            Key = "virtual-machines",
            DisplayName = "Virtual Machines",
            Description = "Cómputo, sistema operativo, red, discos y extensiones",
            Category = "Compute",
            Icon = "🖥️",
            SourceName = "dbo.VMInventoryCurrent",
            IsSpecialized = true
        },
        new()
        {
            Key = "managed-disks",
            DisplayName = "Managed Disks",
            Description = "Discos OS, Data y almacenamiento temporal",
            Category = "Compute",
            Icon = "💽",
            SourceName = "dbo.VMDiskInventoryCurrent"
        }
    ];

    private static readonly IReadOnlyDictionary<string, ServicePresentation>
        ServicePresentations =
            new Dictionary<string, ServicePresentation>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["API Management"] = new(
                    "Integration",
                    "🔌",
                    "Servicios API Management, SKU y configuración"),

                ["App Service"] = new(
                    "Web & Containers",
                    "🌐",
                    "Web Apps, planes, runtime y configuración"),

                ["Application Gateway"] = new(
                    "Networking",
                    "🚪",
                    "Application Gateways, WAF, SKU y estado"),

                ["Azure Container Registry"] = new(
                    "Web & Containers",
                    "📦",
                    "Registros ACR, SKU y configuración"),

                ["Azure Cosmos DB"] = new(
                    "Databases",
                    "🌌",
                    "Cuentas Cosmos DB, ubicación, SKU y estado"),

                ["Azure Data Factory"] = new(
                    "Integration",
                    "🏭",
                    "Factories, integración y configuración"),

                ["Azure Fabric Capacity"] = new(
                    "Analytics & AI",
                    "📊",
                    "Capacidades de Microsoft Fabric, SKU y estado"),

                ["Azure Function"] = new(
                    "Web & Containers",
                    "⚡",
                    "Function Apps, runtime, hosting y estado"),

                ["Azure SQL Database"] = new(
                    "Databases",
                    "🗄️",
                    "Servidores SQL, bases de datos, SKU y capacidad"),

                ["PostgreSQL Flexible Server"] = new(
                    "Databases",
                    "🐘",
                    "Servidores PostgreSQL, versión y capacidad"),

                ["Redis Enterprise"] = new(
                    "Databases",
                    "🚀",
                    "Instancias Redis Enterprise, SKU y capacidad"),

                ["SQL Managed Instance"] = new(
                    "Databases",
                    "🏢",
                    "Instancias administradas, capacidad y estado")
            };

    public AzureInventoryCatalogService(
        ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AzureInventoryModule>>
        GetAvailableModulesAsync(
            string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return [];
        }

        var normalizedSubscriptionId =
            subscriptionId.Trim();

        var result =
            new List<AzureInventoryModule>();

        using var connection =
            _connectionFactory.CreateConnection();

        connection.Open();

        foreach (var definition in SpecializedCatalog)
        {
            var module =
                Copy(definition);

            module.ResourceCount =
                await GetSpecializedResourceCountAsync(
                    connection,
                    module.SourceName,
                    normalizedSubscriptionId);

            if (module.IsAvailable)
            {
                result.Add(module);
            }
        }

        var serviceModules =
            await GetServiceModulesAsync(
                connection,
                normalizedSubscriptionId);

        result.AddRange(serviceModules);

        return result
            .OrderBy(module =>
                CategoryOrder(module.Category))
            .ThenBy(module =>
                module.DisplayName)
            .ToList();
    }

    public AzureInventoryModule? GetModule(
        string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var specialized =
            SpecializedCatalog.FirstOrDefault(
                module =>
                    string.Equals(
                        module.Key,
                        key,
                        StringComparison.OrdinalIgnoreCase));

        if (specialized is not null)
        {
            return Copy(specialized);
        }

        var resourceSubType =
            DecodeResourceSubType(key);

        if (string.IsNullOrWhiteSpace(resourceSubType))
        {
            return null;
        }

        return CreateServiceModule(
            resourceSubType,
            0);
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>>
        GetRowsAsync(
            string moduleKey,
            string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(moduleKey) ||
            string.IsNullOrWhiteSpace(subscriptionId))
        {
            return [];
        }

        using var connection =
            _connectionFactory.CreateConnection();

        connection.Open();

        if (IsDynamicServiceKey(moduleKey))
        {
            var resourceSubType =
                DecodeResourceSubType(moduleKey);

            if (string.IsNullOrWhiteSpace(resourceSubType))
            {
                return [];
            }

            return await GetServiceRowsAsync(
                connection,
                subscriptionId.Trim(),
                resourceSubType);
        }

        var module =
            GetModule(moduleKey);

        if (module is null)
        {
            return [];
        }

        return await GetSpecializedRowsAsync(
            connection,
            module.SourceName,
            subscriptionId.Trim());
    }

    private static async Task<IReadOnlyList<AzureInventoryModule>>
        GetServiceModulesAsync(
            IDbConnection connection,
            string subscriptionId)
    {
        if (!await ObjectExistsAsync(
                connection,
                UnifiedServicesSource))
        {
            return [];
        }

        const string sql = """
            SELECT
                LTRIM(RTRIM(ResourceSubType))
                    AS ResourceSubType,
                COUNT_BIG(*) AS ResourceCount
            FROM dbo.AzureServicesInventoryCurrent
            WHERE
                TRY_CONVERT(
                    uniqueidentifier,
                    SubscriptionId
                ) =
                TRY_CONVERT(
                    uniqueidentifier,
                    @SubscriptionId
                )
                AND IsActive = 1
                AND NULLIF(
                    LTRIM(RTRIM(ResourceSubType)),
                    ''
                ) IS NOT NULL
            GROUP BY
                LTRIM(RTRIM(ResourceSubType));
            """;

        var rows =
            await connection.QueryAsync<ServiceCountRow>(
                sql,
                new
                {
                    SubscriptionId = subscriptionId
                });

        return rows
            .Where(row =>
                !string.IsNullOrWhiteSpace(
                    row.ResourceSubType))
            .Select(row =>
                CreateServiceModule(
                    row.ResourceSubType!,
                    row.ResourceCount > int.MaxValue
                        ? int.MaxValue
                        : Convert.ToInt32(
                            row.ResourceCount)))
            .ToList();
    }

    private static async Task<
        IReadOnlyList<Dictionary<string, object?>>>
        GetServiceRowsAsync(
            IDbConnection connection,
            string subscriptionId,
            string resourceSubType)
    {
        const string sql = """
            SELECT TOP (5000) *
            FROM dbo.AzureServicesInventoryCurrent
            WHERE
                TRY_CONVERT(
                    uniqueidentifier,
                    SubscriptionId
                ) =
                TRY_CONVERT(
                    uniqueidentifier,
                    @SubscriptionId
                )
                AND IsActive = 1
                AND LTRIM(RTRIM(ResourceSubType)) =
                    @ResourceSubType
            ORDER BY
                ResourceName;
            """;

        var rows =
            await connection.QueryAsync(
                sql,
                new
                {
                    SubscriptionId = subscriptionId,
                    ResourceSubType =
                        resourceSubType.Trim()
                });

        return rows
            .Select(ToDictionary)
            .ToList();
    }

    private static async Task<
        IReadOnlyList<Dictionary<string, object?>>>
        GetSpecializedRowsAsync(
            IDbConnection connection,
            string sourceName,
            string subscriptionId)
    {
        if (!await ObjectExistsAsync(
                connection,
                sourceName))
        {
            return [];
        }

        var columns =
            await GetColumnNamesAsync(
                connection,
                sourceName);

        var whereClause =
            BuildSubscriptionFilter(columns);

        var sql = $"""
            SELECT TOP (5000) *
            FROM {sourceName}
            {whereClause}
            ORDER BY 1;
            """;

        var rows =
            await connection.QueryAsync(
                sql,
                new
                {
                    SubscriptionId = subscriptionId
                });

        return rows
            .Select(ToDictionary)
            .ToList();
    }

    private static async Task<int>
        GetSpecializedResourceCountAsync(
            IDbConnection connection,
            string sourceName,
            string subscriptionId)
    {
        if (!await ObjectExistsAsync(
                connection,
                sourceName))
        {
            return 0;
        }

        var columns =
            await GetColumnNamesAsync(
                connection,
                sourceName);

        var whereClause =
            BuildSubscriptionFilter(columns);

        var sql = $"""
            SELECT COUNT_BIG(*)
            FROM {sourceName}
            {whereClause};
            """;

        if (string.Equals(
                sourceName,
                "dbo.VMDiskInventoryCurrent",
                StringComparison.OrdinalIgnoreCase))
        {
            sql = $"""
                SELECT COUNT_BIG(
                    DISTINCT VMResourceId
                )
                FROM {sourceName}
                {whereClause};
                """;
        }

        var count =
            await connection.ExecuteScalarAsync<long>(
                sql,
                new
                {
                    SubscriptionId = subscriptionId
                });

        return count > int.MaxValue
            ? int.MaxValue
            : Convert.ToInt32(count);
    }

    private static AzureInventoryModule
        CreateServiceModule(
            string resourceSubType,
            int resourceCount)
    {
        var normalizedName =
            resourceSubType.Trim();

        var presentation =
            ServicePresentations.TryGetValue(
                normalizedName,
                out var configured)
                    ? configured
                    : new ServicePresentation(
                        "Other",
                        "☁️",
                        $"Inventario de {normalizedName}");

        return new AzureInventoryModule
        {
            Key =
                EncodeResourceSubType(
                    normalizedName),

            DisplayName =
                normalizedName,

            Description =
                presentation.Description,

            Category =
                presentation.Category,

            Icon =
                presentation.Icon,

            SourceName =
                UnifiedServicesSource,

            IsSpecialized =
                false,

            ResourceCount =
                resourceCount
        };
    }

    private static bool IsDynamicServiceKey(
        string key)
    {
        return key.StartsWith(
            DynamicKeyPrefix,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string EncodeResourceSubType(
        string resourceSubType)
    {
        var bytes =
            Encoding.UTF8.GetBytes(
                resourceSubType);

        var encoded =
            Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        return DynamicKeyPrefix + encoded;
    }

    private static string? DecodeResourceSubType(
        string key)
    {
        if (!IsDynamicServiceKey(key))
        {
            return null;
        }

        try
        {
            var encoded =
                key[DynamicKeyPrefix.Length..]
                    .Replace('-', '+')
                    .Replace('_', '/');

            encoded =
                encoded.PadRight(
                    encoded.Length +
                    ((4 - encoded.Length % 4) % 4),
                    '=');

            var bytes =
                Convert.FromBase64String(encoded);

            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static async Task<bool>
        ObjectExistsAsync(
            IDbConnection connection,
            string sourceName)
    {
        const string sql = """
            SELECT CASE
                WHEN OBJECT_ID(@SourceName) IS NULL
                    THEN 0
                ELSE 1
            END;
            """;

        return await connection
            .ExecuteScalarAsync<int>(
                sql,
                new
                {
                    SourceName = sourceName
                }) == 1;
    }

    private static async Task<HashSet<string>>
        GetColumnNamesAsync(
            IDbConnection connection,
            string sourceName)
    {
        const string sql = """
            SELECT c.name
            FROM sys.columns AS c
            WHERE c.object_id =
                OBJECT_ID(@SourceName);
            """;

        var names =
            await connection.QueryAsync<string>(
                sql,
                new
                {
                    SourceName = sourceName
                });

        return names.ToHashSet(
            StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildSubscriptionFilter(
        IReadOnlySet<string> columns)
    {
        if (!columns.Contains("SubscriptionId"))
        {
            return string.Empty;
        }

        var conditions =
            new List<string>
            {
                """
                TRY_CONVERT(
                    uniqueidentifier,
                    SubscriptionId
                ) =
                TRY_CONVERT(
                    uniqueidentifier,
                    @SubscriptionId
                )
                """
            };

        if (columns.Contains("IsActive"))
        {
            conditions.Add(
                "IsActive = 1");
        }

        return "WHERE " +
               string.Join(
                   Environment.NewLine +
                   " AND ",
                   conditions);
    }

    private static Dictionary<string, object?>
        ToDictionary(
            dynamic row)
    {
        var source =
            (IDictionary<string, object>)row;

        return source.ToDictionary(
            item => item.Key,
            item => (object?)item.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static AzureInventoryModule Copy(
        AzureInventoryModule source)
    {
        return new AzureInventoryModule
        {
            Key = source.Key,
            DisplayName = source.DisplayName,
            Description = source.Description,
            Category = source.Category,
            Icon = source.Icon,
            SourceName = source.SourceName,
            ResourceSubType = source.ResourceSubType,
            IsSpecialized = source.IsSpecialized,
            ResourceCount = source.ResourceCount
        };
    }

    private static int CategoryOrder(
        string category)
    {
        return category switch
        {
            "Compute" => 1,
            "Networking" => 2,
            "Web & Containers" => 3,
            "Databases" => 4,
            "Integration" => 5,
            "Analytics & AI" => 6,
            "Management" => 7,
            "Other" => 98,
            _ => 99
        };
    }

    private sealed record ServicePresentation(
        string Category,
        string Icon,
        string Description);

    private sealed class ServiceCountRow
    {
        public string? ResourceSubType
        {
            get;
            init;
        }

        public long ResourceCount
        {
            get;
            init;
        }
    }
}






