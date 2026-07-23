using System.Data;
using Dapper;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models.Infrastructure;
using ITQS.SupportOperationsCenter.Data;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services.Infrastructure;

public sealed class AzureInventoryCatalogService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    private static readonly IReadOnlyList<AzureInventoryModule> Catalog =
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
        },
        new()
        {
            Key = "vm-scale-sets",
            DisplayName = "Virtual Machine Scale Sets",
            Description = "Conjuntos de escalado y capacidad de cómputo",
            Category = "Compute",
            Icon = "🖥️",
            SourceName = "dbo.VMScaleSetInventoryCurrent"
        },
        new()
        {
            Key = "aks",
            DisplayName = "Azure Kubernetes Service",
            Description = "Clusters AKS, versiones y configuración",
            Category = "Compute",
            Icon = "☸️",
            SourceName = "dbo.AKSInventoryCurrent"
        },
        new()
        {
            Key = "app-services",
            DisplayName = "App Services",
            Description = "Web Apps, planes, runtime y configuración",
            Category = "Web & Containers",
            Icon = "🌐",
            SourceName = "dbo.AppServiceInventoryCurrent"
        },
        new()
        {
            Key = "azure-functions",
            DisplayName = "Azure Functions",
            Description = "Function Apps, runtime, hosting y estado",
            Category = "Web & Containers",
            Icon = "⚡",
            SourceName = "dbo.AzureFunctionInventoryCurrent"
        },
        new()
        {
            Key = "container-registry",
            DisplayName = "Container Registry",
            Description = "Registros ACR, SKU y configuración",
            Category = "Web & Containers",
            Icon = "📦",
            SourceName = "dbo.ContainerRegistryInventoryCurrent"
        },
        new()
        {
            Key = "sql-databases",
            DisplayName = "Azure SQL Database",
            Description = "Servidores SQL, bases de datos, SKU y capacidad",
            Category = "Databases",
            Icon = "🗄️",
            SourceName = "dbo.AzureSqlDatabaseInventoryCurrent"
        },
        new()
        {
            Key = "sql-managed-instance",
            DisplayName = "SQL Managed Instance",
            Description = "Instancias administradas, capacidad y estado",
            Category = "Databases",
            Icon = "🏢",
            SourceName = "dbo.SqlManagedInstanceInventoryCurrent"
        },
        new()
        {
            Key = "postgresql",
            DisplayName = "Azure Database for PostgreSQL",
            Description = "Servidores PostgreSQL, versiones y capacidad",
            Category = "Databases",
            Icon = "🐘",
            SourceName = "dbo.PostgreSqlInventoryCurrent"
        },
        new()
        {
            Key = "cosmos-db",
            DisplayName = "Cosmos DB",
            Description = "Cuentas Cosmos DB, API, regiones y consistencia",
            Category = "Databases",
            Icon = "🌌",
            SourceName = "dbo.CosmosDbInventoryCurrent"
        },
        new()
        {
            Key = "redis",
            DisplayName = "Azure Cache for Redis",
            Description = "Cachés Redis, SKU, capacidad y estado",
            Category = "Databases",
            Icon = "🚀",
            SourceName = "dbo.RedisCacheInventoryCurrent"
        },
        new()
        {
            Key = "application-gateway",
            DisplayName = "Application Gateway",
            Description = "Gateways, WAF, listeners y backends",
            Category = "Networking",
            Icon = "🚪",
            SourceName = "dbo.ApplicationGatewayInventoryCurrent"
        },
        new()
        {
            Key = "api-management",
            DisplayName = "API Management",
            Description = "Servicios APIM, SKU y configuración",
            Category = "Integration",
            Icon = "🔌",
            SourceName = "dbo.ApiManagementInventoryCurrent"
        },
        new()
        {
            Key = "data-factory",
            DisplayName = "Data Factory",
            Description = "Factories, integración y configuración",
            Category = "Integration",
            Icon = "🏭",
            SourceName = "dbo.DataFactoryInventoryCurrent"
        },
        new()
        {
            Key = "synapse",
            DisplayName = "Synapse Analytics",
            Description = "Workspaces, pools y componentes analíticos",
            Category = "Analytics & AI",
            Icon = "📈",
            SourceName = "dbo.SynapseInventoryCurrent"
        },
        new()
        {
            Key = "fabric",
            DisplayName = "Microsoft Fabric",
            Description = "Capacidades y elementos de Microsoft Fabric",
            Category = "Analytics & AI",
            Icon = "📊",
            SourceName = "dbo.FabricInventoryCurrent"
        },
        new()
        {
            Key = "azure-ai",
            DisplayName = "Azure AI Services",
            Description = "Servicios cognitivos y recursos de inteligencia artificial",
            Category = "Analytics & AI",
            Icon = "🤖",
            SourceName = "dbo.AzureAIInventoryCurrent"
        }
    ];

    public AzureInventoryCatalogService(
        ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AzureInventoryModule>> GetAvailableModulesAsync(
        string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return [];
        }

        var result = new List<AzureInventoryModule>();

        using var connection = _connectionFactory.CreateConnection();

        connection.Open();

        foreach (var definition in Catalog)
        {
            var module = Copy(definition);

            module.ResourceCount =
                await GetResourceCountAsync(
                    connection,
                    module.SourceName,
                    subscriptionId.Trim());

            if (module.IsAvailable)
            {
                result.Add(module);
            }
        }

        return result
            .OrderBy(module => CategoryOrder(module.Category))
            .ThenBy(module => module.DisplayName)
            .ToList();
    }

    public AzureInventoryModule? GetModule(string key)
    {
        var definition = Catalog.FirstOrDefault(
            module => string.Equals(
                module.Key,
                key,
                StringComparison.OrdinalIgnoreCase));

        return definition is null
            ? null
            : Copy(definition);
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetRowsAsync(
        string moduleKey,
        string subscriptionId)
    {
        var module = GetModule(moduleKey);

        if (module is null ||
            string.IsNullOrWhiteSpace(subscriptionId))
        {
            return [];
        }

        using var connection = _connectionFactory.CreateConnection();

        connection.Open();

        if (!await ObjectExistsAsync(connection, module.SourceName))
        {
            return [];
        }

        var columns =
            await GetColumnNamesAsync(
                connection,
                module.SourceName);

        var whereClause =
            BuildSubscriptionFilter(columns);

        var sql = $"""
            SELECT TOP (5000) *
            FROM {module.SourceName}
            {whereClause}
            ORDER BY 1;
            """;

        var rows = await connection.QueryAsync(
            sql,
            new
            {
                SubscriptionId = subscriptionId.Trim()
            });

        return rows
            .Select(ToDictionary)
            .ToList();
    }

    private static async Task<int> GetResourceCountAsync(
        IDbConnection connection,
        string sourceName,
        string subscriptionId)
    {
        if (!await ObjectExistsAsync(connection, sourceName))
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

        var count = await connection.ExecuteScalarAsync<long>(
            sql,
            new
            {
                SubscriptionId = subscriptionId
            });

        return count > int.MaxValue
            ? int.MaxValue
            : Convert.ToInt32(count);
    }

    private static async Task<bool> ObjectExistsAsync(
        IDbConnection connection,
        string sourceName)
    {
        const string sql = """
            SELECT CASE
                WHEN OBJECT_ID(@SourceName) IS NULL THEN 0
                ELSE 1
            END;
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                SourceName = sourceName
            }) == 1;
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(
        IDbConnection connection,
        string sourceName)
    {
        const string sql = """
            SELECT c.name
            FROM sys.columns AS c
            WHERE c.object_id = OBJECT_ID(@SourceName);
            """;

        var names = await connection.QueryAsync<string>(
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

        var conditions = new List<string>
        {
            """
            TRY_CONVERT(uniqueidentifier, SubscriptionId) =
            TRY_CONVERT(uniqueidentifier, @SubscriptionId)
            """
        };

        if (columns.Contains("IsActive"))
        {
            conditions.Add("IsActive = 1");
        }

        return "WHERE " + string.Join(
            Environment.NewLine + " AND ",
            conditions);
    }

    private static Dictionary<string, object?> ToDictionary(
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
            IsSpecialized = source.IsSpecialized,
            ResourceCount = source.ResourceCount
        };
    }

    private static int CategoryOrder(string category)
    {
        return category switch
        {
            "Compute" => 1,
            "Networking" => 2,
            "Web & Containers" => 3,
            "Databases" => 4,
            "Integration" => 5,
            "Analytics & AI" => 6,
            _ => 99
        };
    }
}
