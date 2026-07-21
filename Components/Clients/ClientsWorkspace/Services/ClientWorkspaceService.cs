using System.Data;
using Dapper;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Models;

namespace ITQS.SupportOperationsCenter.Components.Clients.ClientsWorkspace.Services;

public sealed class ClientWorkspaceService : IClientWorkspaceService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ClientWorkspaceService(
        ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(
                nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<ClientWorkspaceCustomer>> GetCustomersAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH OperationalSources AS
            (
                SELECT
                    TenantId,
                    MAX(LastSeenAt) AS LastOperationalUpdate
                FROM dbo.VMInventoryCurrent
                GROUP BY TenantId

                UNION ALL

                SELECT
                    TenantId,
                    MAX(FechaRegistro)
                FROM dbo.InfrastructureHealthDaily
                GROUP BY TenantId

                UNION ALL

                SELECT
                    TenantId,
                    MAX(FechaRegistro)
                FROM dbo.VmPerformanceDaily
                GROUP BY TenantId

                UNION ALL

                SELECT
                    TenantId,
                    MAX(AdvisorUpdatedAt)
                FROM dbo.vw_AdvisorRecommendationsValued
                GROUP BY TenantId
            ),
            OperationalSummary AS
            (
                SELECT
                    TenantId,
                    MAX(LastOperationalUpdate) AS LastOperationalUpdate
                FROM OperationalSources
                GROUP BY TenantId
            ),
            SubscriptionSources AS
            (
                SELECT
                    TenantId,
                    TRY_CONVERT(uniqueidentifier, SubscriptionId)
                        AS SubscriptionId
                FROM dbo.ITQS_Subscriptions

                UNION

                SELECT
                    TenantId,
                    SubscriptionId
                FROM dbo.Subscriptions
                WHERE TenantId IS NOT NULL
                  AND SubscriptionId IS NOT NULL

                UNION

                SELECT
                    TenantId,
                    TRY_CONVERT(uniqueidentifier, SubscriptionId)
                FROM dbo.VMInventoryCurrent
                WHERE TRY_CONVERT(uniqueidentifier, SubscriptionId)
                      IS NOT NULL

                UNION

                SELECT
                    TenantId,
                    SubscriptionId
                FROM dbo.InfrastructureHealthDaily

                UNION

                SELECT
                    TenantId,
                    SubscriptionId
                FROM dbo.VmPerformanceDaily

                UNION

                SELECT
                    TenantId,
                    SubscriptionId
                FROM dbo.StorageAccountMetricsDaily

                UNION

                SELECT
                    TenantId,
                    SubscriptionId
                FROM dbo.vw_AdvisorRecommendationsValued

                UNION

                SELECT
                    TenantId,
                    SubscriptionId
                FROM dbo.vw_BackupInventoryBySubscription
                WHERE TenantId IS NOT NULL
                  AND SubscriptionId IS NOT NULL
            ),
            SubscriptionSummary AS
            (
                SELECT
                    TenantId,
                    COUNT(DISTINCT SubscriptionId) AS SubscriptionCount
                FROM SubscriptionSources
                WHERE SubscriptionId IS NOT NULL
                GROUP BY TenantId
            ),
            PartnerSource AS
            (
                SELECT
                    TRY_CONVERT(
                        uniqueidentifier,
                        PC.CustomerTenantId
                    ) AS TenantId,
                    PC.CustomerName AS PartnerCenterCustomerName,
                    PC.PartnerTenant,
                    PC.StatusFound,
                    CRM.AccountNameCRM,
                    CRM.CustomerDomainPc,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY
                            TRY_CONVERT(
                                uniqueidentifier,
                                PC.CustomerTenantId
                            )
                        ORDER BY
                            PC.ExecutionDate DESC,
                            PC.Id DESC
                    ) AS RowNumber
                FROM dbo.PartnerCenterCustomers AS PC
                LEFT JOIN dbo.CRM_Customers AS CRM
                    ON TRY_CONVERT(
                           uniqueidentifier,
                           CRM.MicrosoftID
                       ) =
                       TRY_CONVERT(
                           uniqueidentifier,
                           PC.CustomerTenantId
                       )
                WHERE TRY_CONVERT(
                          uniqueidentifier,
                          PC.CustomerTenantId
                      ) IS NOT NULL
            )
            SELECT
                C.TenantId,

                COALESCE(
                    NULLIF(LTRIM(RTRIM(P.AccountNameCRM)), ''),
                    NULLIF(LTRIM(RTRIM(C.CustomerNamePortal)), ''),
                    NULLIF(LTRIM(RTRIM(C.CustomerName)), ''),
                    'Cliente sin nombre'
                ) AS CustomerName,

                ISNULL(C.CustomerNamePortal, '') AS CustomerNamePortal,

                COALESCE(
                    NULLIF(LTRIM(RTRIM(P.CustomerDomainPc)), ''),
                    NULLIF(
                        LTRIM(
                            RTRIM(
                                REPLACE(
                                    P.PartnerCenterCustomerName,
                                    ' Tenant',
                                    ''
                                )
                            )
                        ),
                        ''
                    ),
                    NULLIF(LTRIM(RTRIM(C.CustomerNamePortal)), ''),
                    NULLIF(LTRIM(RTRIM(C.CustomerName)), ''),
                    'Sin suscripción'
                ) AS SubscriptionName,

                CONVERT(bit, ISNULL(C.IsActive, 0)) AS IsActive,
                ISNULL(P.PartnerTenant, '') AS PartnerTenant,
                ISNULL(P.StatusFound, '') AS GdapStatus,
                ISNULL(S.SubscriptionCount, 0) AS SubscriptionCount,
                O.LastOperationalUpdate
            FROM dbo.ITQS_Customers AS C
            LEFT JOIN SubscriptionSummary AS S
                ON S.TenantId = C.TenantId
            LEFT JOIN OperationalSummary AS O
                ON O.TenantId = C.TenantId
            LEFT JOIN PartnerSource AS P
                ON P.TenantId = C.TenantId
               AND P.RowNumber = 1
            WHERE ISNULL(C.IsActive, 0) = 1
            ORDER BY
                COALESCE(
                    NULLIF(LTRIM(RTRIM(P.AccountNameCRM)), ''),
                    NULLIF(LTRIM(RTRIM(C.CustomerNamePortal)), ''),
                    NULLIF(LTRIM(RTRIM(C.CustomerName)), '')
                );
            """;

        using var connection =
            _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            cancellationToken: cancellationToken,
            commandTimeout: 120);

        var result =
            await connection.QueryAsync<ClientWorkspaceCustomer>(command);

        return result.AsList();
    }

    public async Task<IReadOnlyList<ClientWorkspaceSubscription>>
        GetSubscriptionsAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH SubscriptionSources AS
            (
                SELECT
                    TenantId,
                    TRY_CONVERT(uniqueidentifier, SubscriptionId)
                        AS SubscriptionId,
                    SubscriptionName,
                    CAST(NULL AS nvarchar(50)) AS State,
                    LastSeenAt
                FROM dbo.ITQS_Subscriptions
                WHERE TenantId = @TenantId

                UNION ALL

                SELECT
                    TenantId,
                    SubscriptionId,
                    SubscriptionName,
                    State,
                    COALESCE(InsertedAt, FechaRegistro)
                FROM dbo.Subscriptions
                WHERE TenantId = @TenantId
                  AND SubscriptionId IS NOT NULL

                UNION ALL

                SELECT
                    TenantId,
                    TRY_CONVERT(uniqueidentifier, SubscriptionId),
                    SubscriptionName,
                    NULL,
                    LastSeenAt
                FROM dbo.VMInventoryCurrent
                WHERE TenantId = @TenantId
                  AND TRY_CONVERT(
                          uniqueidentifier,
                          SubscriptionId
                      ) IS NOT NULL

                UNION ALL

                SELECT
                    TenantId,
                    SubscriptionId,
                    SubscriptionName,
                    OverallStatus,
                    FechaRegistro
                FROM dbo.InfrastructureHealthDaily
                WHERE TenantId = @TenantId

                UNION ALL

                SELECT
                    TenantId,
                    SubscriptionId,
                    SubscriptionName,
                    VMStatus,
                    FechaRegistro
                FROM dbo.VmPerformanceDaily
                WHERE TenantId = @TenantId

                UNION ALL

                SELECT
                    TenantId,
                    SubscriptionId,
                    SubscriptionName,
                    StorageStatus,
                    FechaRegistro
                FROM dbo.StorageAccountMetricsDaily
                WHERE TenantId = @TenantId

                UNION ALL

                SELECT
                    TenantId,
                    SubscriptionId,
                    SubscriptionName,
                    NULL,
                    AdvisorUpdatedAt
                FROM dbo.vw_AdvisorRecommendationsValued
                WHERE TenantId = @TenantId

                UNION ALL

                SELECT
                    TenantId,
                    SubscriptionId,
                    SubscriptionName,
                    NULL,
                    LastInventoryDate
                FROM dbo.vw_BackupInventoryBySubscription
                WHERE TenantId = @TenantId
            ),
            Ranked AS
            (
                SELECT
                    TenantId,
                    SubscriptionId,
                    SubscriptionName,
                    State,
                    LastSeenAt,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY SubscriptionId
                        ORDER BY
                            CASE
                                WHEN NULLIF(
                                    LTRIM(RTRIM(SubscriptionName)),
                                    ''
                                ) IS NOT NULL
                                THEN 0
                                ELSE 1
                            END,
                            LastSeenAt DESC
                    ) AS RowNumber
                FROM SubscriptionSources
                WHERE SubscriptionId IS NOT NULL
            ),
            Aggregated AS
            (
                SELECT
                    TenantId,
                    SubscriptionId,
                    MAX(
                        NULLIF(
                            LTRIM(RTRIM(SubscriptionName)),
                            ''
                        )
                    ) AS SubscriptionName,
                    MAX(
                        NULLIF(
                            LTRIM(RTRIM(State)),
                            ''
                        )
                    ) AS State,
                    MAX(LastSeenAt) AS LastSeenAt
                FROM Ranked
                GROUP BY
                    TenantId,
                    SubscriptionId
            )
            SELECT
                TenantId,
                SubscriptionId,
                ISNULL(
                    SubscriptionName,
                    CONVERT(nvarchar(36), SubscriptionId)
                ) AS SubscriptionName,
                ISNULL(State, 'Disponible') AS State,
                LastSeenAt
            FROM Aggregated
            ORDER BY
                SubscriptionName;
            """;

        using var connection =
            _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                TenantId = tenantId
            },
            cancellationToken: cancellationToken,
            commandTimeout: 120);

        var result =
            await connection.QueryAsync<ClientWorkspaceSubscription>(
                command);

        return result.AsList();
    }

    public async Task<ClientWorkspaceOverview> GetOverviewAsync(
        Guid tenantId,
        Guid? subscriptionId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DECLARE @SubscriptionText nvarchar(100) =
                CASE
                    WHEN @SubscriptionId IS NULL THEN NULL
                    ELSE CONVERT(nvarchar(36), @SubscriptionId)
                END;

            SELECT
                TotalSubscriptions =
                (
                    SELECT COUNT(DISTINCT X.SubscriptionId)
                    FROM
                    (
                        SELECT
                            TRY_CONVERT(
                                uniqueidentifier,
                                SubscriptionId
                            ) AS SubscriptionId
                        FROM dbo.VMInventoryCurrent
                        WHERE TenantId = @TenantId

                        UNION

                        SELECT SubscriptionId
                        FROM dbo.InfrastructureHealthDaily
                        WHERE TenantId = @TenantId

                        UNION

                        SELECT SubscriptionId
                        FROM dbo.VmPerformanceDaily
                        WHERE TenantId = @TenantId

                        UNION

                        SELECT SubscriptionId
                        FROM dbo.StorageAccountMetricsDaily
                        WHERE TenantId = @TenantId
                    ) AS X
                    WHERE X.SubscriptionId IS NOT NULL
                ),

                TotalVirtualMachines =
                (
                    SELECT COUNT_BIG(*)
                    FROM dbo.VMInventoryCurrent
                    WHERE TenantId = @TenantId
                      AND IsActive = 1
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR TRY_CONVERT(
                                 uniqueidentifier,
                                 SubscriptionId
                             ) = @SubscriptionId
                      )
                ),

                TotalDisks =
                (
                    SELECT COUNT_BIG(*)
                    FROM dbo.VMDiskInventoryCurrent
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR TRY_CONVERT(
                                 uniqueidentifier,
                                 SubscriptionId
                             ) = @SubscriptionId
                      )
                ),

                TotalAdvisorRecommendations =
                (
                    SELECT COUNT_BIG(*)
                    FROM dbo.vw_AdvisorRecommendationsValued
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId = @SubscriptionId
                      )
                ),

                AnnualAdvisorSavings =
                (
                    SELECT ISNULL(
                        SUM(AnnualSavingsAmount),
                        0
                    )
                    FROM dbo.vw_AdvisorRecommendationsValued
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId = @SubscriptionId
                      )
                ),

                TotalBackupVirtualMachines =
                (
                    SELECT ISNULL(SUM(TotalVMs), 0)
                    FROM dbo.vw_BackupInventoryBySubscription
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId = @SubscriptionId
                      )
                ),

                VirtualMachinesWithBackup =
                (
                    SELECT ISNULL(SUM(VMsConBackup), 0)
                    FROM dbo.vw_BackupInventoryBySubscription
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId = @SubscriptionId
                      )
                ),

                VirtualMachinesWithoutBackup =
                (
                    SELECT ISNULL(SUM(VMsSinBackup), 0)
                    FROM dbo.vw_BackupInventoryBySubscription
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId = @SubscriptionId
                      )
                ),

                BackupCoveragePercentage =
                (
                    SELECT CAST(
                        CASE
                            WHEN ISNULL(SUM(TotalVMs), 0) = 0
                                THEN 0
                            ELSE
                                SUM(VMsConBackup) * 100.0
                                / SUM(TotalVMs)
                        END
                        AS decimal(10,2)
                    )
                    FROM dbo.vw_BackupInventoryBySubscription
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId = @SubscriptionId
                      )
                ),

                OrphanResources =
                (
                    SELECT
                        (
                            SELECT COUNT_BIG(*)
                            FROM dbo.NICsDetached
                            WHERE TenantId = @TenantId
                              AND
                              (
                                  @SubscriptionId IS NULL
                                  OR SubscriptionId =
                                     CONVERT(
                                         varchar(36),
                                         @SubscriptionId
                                     )
                              )
                        )
                        +
                        (
                            SELECT COUNT_BIG(*)
                            FROM dbo.NSGsDetached
                            WHERE TenantId = @TenantId
                              AND
                              (
                                  @SubscriptionId IS NULL
                                  OR SubscriptionId =
                                     CONVERT(
                                         varchar(36),
                                         @SubscriptionId
                                     )
                              )
                        )
                        +
                        (
                            SELECT COUNT_BIG(*)
                            FROM dbo.PublicIPsDetached
                            WHERE TenantId = @TenantId
                              AND
                              (
                                  @SubscriptionId IS NULL
                                  OR SubscriptionId =
                                     CONVERT(
                                         varchar(36),
                                         @SubscriptionId
                                     )
                              )
                        )
                ),

                ResourcesWithoutTags =
                (
                    SELECT COUNT_BIG(*)
                    FROM dbo.ResourcesWithoutTags
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId =
                             CONVERT(
                                 nvarchar(36),
                                 @SubscriptionId
                             )
                      )
                ),
                CpuCriticalVirtualMachines =
                (
                    SELECT ISNULL(MAX(DailyValue), 0)
                    FROM
                    (
                        SELECT
                            FechaMetrica,
                            SUM(ISNULL(CpuCriticalVMs, 0)) AS DailyValue
                        FROM dbo.InfrastructureHealthDaily
                        WHERE TenantId = @TenantId
                          AND
                          (
                              @SubscriptionId IS NULL
                              OR SubscriptionId = @SubscriptionId
                          )
                          AND FechaMetrica >=
                              DATEADD(
                                  DAY,
                                  -6,
                                  CONVERT(date, GETUTCDATE())
                              )
                        GROUP BY FechaMetrica
                    ) AS DailyCpuCritical
                ),

                MemoryCriticalVirtualMachines =
                (
                    SELECT ISNULL(MAX(DailyValue), 0)
                    FROM
                    (
                        SELECT
                            FechaMetrica,
                            SUM(ISNULL(MemoryCriticalVMs, 0)) AS DailyValue
                        FROM dbo.InfrastructureHealthDaily
                        WHERE TenantId = @TenantId
                          AND
                          (
                              @SubscriptionId IS NULL
                              OR SubscriptionId = @SubscriptionId
                          )
                          AND FechaMetrica >=
                              DATEADD(
                                  DAY,
                                  -6,
                                  CONVERT(date, GETUTCDATE())
                              )
                        GROUP BY FechaMetrica
                    ) AS DailyMemoryCritical
                ),

                DiskCriticalVirtualMachines =
                (
                    SELECT ISNULL(MAX(DailyValue), 0)
                    FROM
                    (
                        SELECT
                            FechaMetrica,
                            SUM(ISNULL(DiskCriticalVMs, 0)) AS DailyValue
                        FROM dbo.InfrastructureHealthDaily
                        WHERE TenantId = @TenantId
                          AND
                          (
                              @SubscriptionId IS NULL
                              OR SubscriptionId = @SubscriptionId
                          )
                          AND FechaMetrica >=
                              DATEADD(
                                  DAY,
                                  -6,
                                  CONVERT(date, GETUTCDATE())
                              )
                        GROUP BY FechaMetrica
                    ) AS DailyDiskCritical
                ),

                AverageCpuPercent =
                (
                    SELECT ISNULL(AVG(CpuAvgPercent), 0)
                    FROM dbo.VmPerformanceDaily
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId = @SubscriptionId
                      )
                      AND FechaMetrica >=
                          DATEADD(DAY, -7, CONVERT(date, GETUTCDATE()))
                ),

                AverageMemoryPercent =
                (
                    SELECT ISNULL(
                        AVG(MemoryUsedAvgPercent),
                        0
                    )
                    FROM dbo.VmPerformanceDaily
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId = @SubscriptionId
                      )
                      AND FechaMetrica >=
                          DATEADD(DAY, -7, CONVERT(date, GETUTCDATE()))
                ),

                AverageAvailabilityPercent =
                (
                    SELECT ISNULL(AVG(AvailabilityRate), 0)
                    FROM dbo.VmPerformanceDaily
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId = @SubscriptionId
                      )
                      AND FechaMetrica >=
                          DATEADD(DAY, -7, CONVERT(date, GETUTCDATE()))
                ),

                OverallStatus =
                (
                    SELECT TOP (1)
                        ISNULL(OverallStatus, 'Sin datos')
                    FROM dbo.InfrastructureHealthDaily
                    WHERE TenantId = @TenantId
                      AND
                      (
                          @SubscriptionId IS NULL
                          OR SubscriptionId = @SubscriptionId
                      )
                    ORDER BY FechaMetrica DESC
                ),

                LastOperationalUpdate =
                (
                    SELECT MAX(X.LastUpdate)
                    FROM
                    (
                        SELECT MAX(LastSeenAt) AS LastUpdate
                        FROM dbo.VMInventoryCurrent
                        WHERE TenantId = @TenantId
                          AND
                          (
                              @SubscriptionId IS NULL
                              OR TRY_CONVERT(
                                     uniqueidentifier,
                                     SubscriptionId
                                 ) = @SubscriptionId
                          )

                        UNION ALL

                        SELECT MAX(FechaRegistro)
                        FROM dbo.InfrastructureHealthDaily
                        WHERE TenantId = @TenantId
                          AND
                          (
                              @SubscriptionId IS NULL
                              OR SubscriptionId = @SubscriptionId
                          )

                        UNION ALL

                        SELECT MAX(FechaRegistro)
                        FROM dbo.VmPerformanceDaily
                        WHERE TenantId = @TenantId
                          AND
                          (
                              @SubscriptionId IS NULL
                              OR SubscriptionId = @SubscriptionId
                          )
                    ) AS X
                );
            """;

        using var connection =
            _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                TenantId = tenantId,
                SubscriptionId = subscriptionId
            },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken,
            commandTimeout: 120);

        return await connection.QuerySingleAsync<ClientWorkspaceOverview>(
            command);
    }
    public async Task<IReadOnlyList<ClientAdvisorRecommendation>>
        GetTopAdvisorRecommendationsAsync(
            Guid tenantId,
            Guid? subscriptionId,
            int top = 10,
            CancellationToken cancellationToken = default)
    {
        top = Math.Clamp(top, 1, 100);

        using var connection =
            _connectionFactory.CreateConnection();

        const string columnsSql = """
            SELECT
                c.name
            FROM sys.columns AS c
            WHERE c.object_id =
                OBJECT_ID(
                    'dbo.vw_AdvisorRecommendationsValued'
                );
            """;

        var columnsCommand = new CommandDefinition(
            columnsSql,
            commandType: CommandType.Text,
            cancellationToken: cancellationToken,
            commandTimeout: 120);

        var availableColumns =
            (await connection.QueryAsync<string>(
                columnsCommand))
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        static string? FindColumn(
            HashSet<string> columns,
            params string[] candidates)
        {
            return candidates.FirstOrDefault(
                columns.Contains);
        }

        static string QuoteColumn(string columnName)
        {
            return $"[{columnName.Replace("]", "]]")}]";
        }

        static string TextExpression(
            string? columnName,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                return $"N'{fallback.Replace("'", "''")}'";
            }

            return
                $"COALESCE(NULLIF(CONVERT(nvarchar(4000), " +
                $"{QuoteColumn(columnName)}), N''), " +
                $"N'{fallback.Replace("'", "''")}')";
        }

        var titleColumn = FindColumn(
            availableColumns,
            "RecommendationName",
            "Recommendation",
            "ShortDescription",
            "RecommendationDescription",
            "Description",
            "Problem",
            "Title");

        var categoryColumn = FindColumn(
            availableColumns,
            "Category",
            "RecommendationCategory",
            "CategoryName");

        var impactColumn = FindColumn(
            availableColumns,
            "Impact",
            "ImpactLevel",
            "BusinessImpact",
            "RiskLevel");

        var resourceColumn = FindColumn(
            availableColumns,
            "ResourceName",
            "ImpactedResourceName",
            "ResourceDisplayName",
            "ImpactedResource",
            "ResourceId");

        var savingsColumn = FindColumn(
            availableColumns,
            "AnnualSavingsAmount");

        var updatedColumn = FindColumn(
            availableColumns,
            "AdvisorUpdatedAt",
            "LastUpdatedAt",
            "UpdatedAt",
            "LastUpdatedDateTime");

        if (savingsColumn is null)
        {
            throw new InvalidOperationException(
                "La vista dbo.vw_AdvisorRecommendationsValued " +
                "no contiene la columna AnnualSavingsAmount.");
        }

        var titleExpression = TextExpression(
            titleColumn,
            "Recomendación Advisor");

        var categoryExpression = TextExpression(
            categoryColumn,
            "Sin categoría");

        var impactExpression = TextExpression(
            impactColumn,
            "Sin clasificar");

        var resourceExpression = TextExpression(
            resourceColumn,
            "-");

        var updatedExpression =
            updatedColumn is null
                ? "CAST(NULL AS datetime2)"
                : $"TRY_CONVERT(datetime2, " +
                  $"{QuoteColumn(updatedColumn)})";

        var savingsExpression =
            $"ISNULL(TRY_CONVERT(decimal(19, 2), " +
            $"{QuoteColumn(savingsColumn)}), 0)";

        var sql = $"""
            SELECT TOP (@Top)
                Title =
                    {titleExpression},

                Category =
                    {categoryExpression},

                Impact =
                    {impactExpression},

                ResourceName =
                    {resourceExpression},

                AnnualSavingsAmount =
                    {savingsExpression},

                UpdatedAt =
                    {updatedExpression}

            FROM dbo.vw_AdvisorRecommendationsValued

            WHERE TenantId = @TenantId
              AND
              (
                  @SubscriptionId IS NULL
                  OR SubscriptionId = @SubscriptionId
              )

            ORDER BY
                {savingsExpression} DESC,
                {updatedExpression} DESC;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                TenantId = tenantId,
                SubscriptionId = subscriptionId,
                Top = top
            },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken,
            commandTimeout: 120);

        var recommendations =
            await connection.QueryAsync<ClientAdvisorRecommendation>(
                command);

        return recommendations.AsList();
    }
    public async Task<IReadOnlyList<ClientAdvisorRecommendation>>
        GetAdvisorRecommendationsAsync(
            Guid tenantId,
            Guid? subscriptionId,
            CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id,
                CustomerName,
                TenantId,
                SubscriptionId,
                SubscriptionName,

                ResourceGroup =
                    ISNULL(ResourceGroup, N''),

                AffectedResource =
                    ISNULL(AffectedResource, N''),

                ResourceName =
                    COALESCE(
                        NULLIF(ImpactedValue, N''),
                        CASE
                            WHEN AffectedResource IS NULL
                                OR AffectedResource = N''
                                THEN N'-'

                            WHEN CHARINDEX(
                                N'/',
                                REVERSE(AffectedResource)
                            ) > 0
                                THEN RIGHT(
                                    AffectedResource,
                                    CHARINDEX(
                                        N'/',
                                        REVERSE(AffectedResource)
                                    ) - 1
                                )

                            ELSE AffectedResource
                        END,
                        N'-'
                    ),

                AzureCategory =
                    COALESCE(
                        NULLIF(AzureCategory, N''),
                        N'Sin categoría'
                    ),

                AdvisorType =
                    ISNULL(AdvisorType, N''),

                Impact =
                    COALESCE(
                        NULLIF(Impact, N''),
                        N'Sin clasificar'
                    ),

                Currency =
                    COALESCE(
                        NULLIF(Currency, N''),
                        N'USD'
                    ),

                AnnualSavingsAmount =
                    ISNULL(AnnualSavingsAmount, 0),

                RecommendationSpanish =
                    COALESCE(
                        NULLIF(RecommendationSpanish, N''),
                        NULLIF(RecommendationAzure, N''),
                        N'Recomendación de Azure Advisor'
                    ),

                DescriptionSpanish =
                    ISNULL(DescriptionSpanish, N''),

                RemediationAzure =
                    ISNULL(RemediationAzure, N''),

                CostClassification =
                    COALESCE(
                        NULLIF(CostClassification, N''),
                        N'Sin clasificar'
                    ),

                RequiresMaintenanceWindow =
                    ISNULL(RequiresMaintenanceWindow, 0),

                ImplementationMinutes =
                    ISNULL(ImplementationMinutes, 0),

                ImplementationHours =
                    ISNULL(ImplementationHours, 0),

                Complexity =
                    COALESCE(
                        NULLIF(Complexity, N''),
                        N'Sin clasificar'
                    ),

                MaintenanceWindowText =
                    COALESCE(
                        NULLIF(MaintenanceWindowText, N''),
                        N'No'
                    ),

                AdvisorUpdatedAt

            FROM dbo.vw_AdvisorRecommendationsValued

            WHERE TenantId = @TenantId
              AND
              (
                  @SubscriptionId IS NULL
                  OR SubscriptionId = @SubscriptionId
              )

            ORDER BY
                CASE UPPER(ISNULL(Impact, N''))
                    WHEN N'HIGH' THEN 1
                    WHEN N'MEDIUM' THEN 2
                    WHEN N'LOW' THEN 3
                    ELSE 4
                END,
                ISNULL(AnnualSavingsAmount, 0) DESC,
                AdvisorUpdatedAt DESC;
            """;

        using var connection =
            _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                TenantId = tenantId,
                SubscriptionId = subscriptionId
            },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken,
            commandTimeout: 120);

        var result =
            await connection.QueryAsync<ClientAdvisorRecommendation>(
                command);

        return result.AsList();
    }

    public async Task<IReadOnlyList<ClientAdvisorScore>>
        GetAdvisorScoresAsync(
            Guid tenantId,
            Guid? subscriptionId,
            CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH LatestScores AS
            (
                SELECT
                    ScoreName,
                    Score,
                    ConsumptionUnits,
                    ImpactedResourceCount,
                    PotentialScoreIncrease,
                    LastScoreDateUtc,
                    LastSeenUtc,

                    RowNumber =
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY
                                TRY_CONVERT(
                                    uniqueidentifier,
                                    SubscriptionId
                                ),
                                ScoreName

                            ORDER BY
                                LastScoreDateUtc DESC,
                                LastSeenUtc DESC
                        )

                FROM dbo.AzureAdvisorScores

                WHERE TRY_CONVERT(
                          uniqueidentifier,
                          TenantId
                      ) = @TenantId

                  AND
                  (
                      @SubscriptionId IS NULL
                      OR TRY_CONVERT(
                             uniqueidentifier,
                             SubscriptionId
                         ) = @SubscriptionId
                  )
            )

            SELECT
                ScoreName,

                Score =
                    CONVERT(
                        decimal(10, 2),
                        AVG(
                            CONVERT(
                                decimal(18, 4),
                                Score
                            )
                        )
                    ),

                ConsumptionUnits =
                    CONVERT(
                        decimal(18, 2),
                        SUM(
                            CONVERT(
                                decimal(18, 4),
                                ISNULL(ConsumptionUnits, 0)
                            )
                        )
                    ),

                ImpactedResourceCount =
                    SUM(ISNULL(ImpactedResourceCount, 0)),

                PotentialScoreIncrease =
                    CONVERT(
                        decimal(10, 2),
                        AVG(
                            CONVERT(
                                decimal(18, 4),
                                ISNULL(PotentialScoreIncrease, 0)
                            )
                        )
                    ),

                LastScoreDateUtc =
                    MAX(LastScoreDateUtc),

                LastSeenUtc =
                    MAX(LastSeenUtc)

            FROM LatestScores

            WHERE RowNumber = 1

            GROUP BY ScoreName

            ORDER BY
                CASE ScoreName
                    WHEN N'Cost' THEN 1
                    WHEN N'HighAvailability' THEN 2
                    WHEN N'OperationalExcellence' THEN 3
                    WHEN N'Performance' THEN 4
                    WHEN N'Security' THEN 5
                    ELSE 6
                END;
            """;

        using var connection =
            _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                TenantId = tenantId,
                SubscriptionId = subscriptionId
            },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken,
            commandTimeout: 120);

        var result =
            await connection.QueryAsync<ClientAdvisorScore>(
                command);

        return result.AsList();
    }

    public async Task<ClientSecurityScore?> GetSecurityScoreAsync(
        Guid tenantId,
        Guid? subscriptionId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH LatestSecurityScores AS
            (
                SELECT
                    SubscriptionId,
                    SecScore,
                    InsertedAt,

                    RowNumber =
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY SubscriptionId
                            ORDER BY
                                InsertedAt DESC,
                                Id DESC
                        )

                FROM dbo.SecureScores

                WHERE TRY_CONVERT(
                          uniqueidentifier,
                          TenantId
                      ) = @TenantId

                  AND
                  (
                      @SubscriptionId IS NULL
                      OR TRY_CONVERT(
                             uniqueidentifier,
                             SubscriptionId
                         ) = @SubscriptionId
                  )
            )

            SELECT
                Score =
                    CONVERT(
                        decimal(10, 2),
                        ISNULL(
                            AVG(
                                CONVERT(
                                    decimal(18, 4),
                                    SecScore
                                )
                            ),
                            0
                        )
                    ),

                SubscriptionCount =
                    COUNT(*),

                UpdatedAt =
                    MAX(InsertedAt)

            FROM LatestSecurityScores

            WHERE RowNumber = 1;
            """;

        using var connection =
            _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                TenantId = tenantId,
                SubscriptionId = subscriptionId
            },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken,
            commandTimeout: 120);

        return await connection
            .QuerySingleOrDefaultAsync<ClientSecurityScore>(
                command);
    }
}

