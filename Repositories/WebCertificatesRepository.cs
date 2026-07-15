using Azure.Security.KeyVault.Secrets;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;
using ITQS.SupportOperationsCenter.Models.Administration.WebCertificates;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class WebCertificatesRepository : IWebCertificatesRepository
{
    private readonly SqlSettings _sqlSettings;
    private readonly KeyVaultSettings _keyVaultSettings;
    private readonly SecretClient _secretClient;

    public WebCertificatesRepository(
        IOptions<SqlSettings> sqlOptions,
        IOptions<KeyVaultSettings> keyVaultOptions,
        SecretClient secretClient)
    {
        _sqlSettings = sqlOptions.Value;
        _keyVaultSettings = keyVaultOptions.Value;
        _secretClient = secretClient;
    }

    public async Task<WebCertificateDashboardModel> GetDashboardAsync()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;

        cmd.CommandText = @"
;WITH LastRun AS
(
    SELECT TOP (1)
        RunId,
        MAX(ProcessedAt) AS LastProcessedAt
    FROM dbo.ITQS_APP_CertInventory_RunCustomers
    GROUP BY RunId
    ORDER BY MAX(ProcessedAt) DESC
),
RunSummary AS
(
    SELECT
        RC.RunId,
        COUNT(*) AS ProcessedCustomers,
        SUM(CASE WHEN RC.CompletedSuccessfully = 1 THEN 1 ELSE 0 END)
            AS SuccessfulCustomers,
        SUM(CASE WHEN RC.CompletedSuccessfully = 0 THEN 1 ELSE 0 END)
            AS FailedCustomers,
        MAX(RC.ProcessedAt) AS LastScanAt
    FROM dbo.ITQS_APP_CertInventory_RunCustomers RC
    INNER JOIN LastRun LR
        ON LR.RunId = RC.RunId
    GROUP BY RC.RunId
),
Operational AS
(
    SELECT C.*
    FROM dbo.ITQS_APP_CertInventory_Current C
    WHERE C.IsPresent = 1
      AND C.IsActive = 1
      AND ISNULL(C.UsageStatus, 'Unknown') = 'Active'
      AND NOT
      (
          C.Source = 'AppServiceCertificateResource'
          AND NULLIF(LTRIM(RTRIM(ISNULL(C.Thumbprint, ''))), '') IS NOT NULL
          AND EXISTS
          (
              SELECT 1
              FROM dbo.ITQS_APP_CertInventory_Current B
              WHERE B.IsPresent = 1
                AND B.IsActive = 1
                AND ISNULL(B.UsageStatus, 'Unknown') = 'Active'
                AND B.Source = 'AppServiceBinding'
                AND ISNULL(B.TenantId, '00000000-0000-0000-0000-000000000000')
                    = ISNULL(C.TenantId, '00000000-0000-0000-0000-000000000000')
                AND ISNULL(B.SubscriptionId, '00000000-0000-0000-0000-000000000000')
                    = ISNULL(C.SubscriptionId, '00000000-0000-0000-0000-000000000000')
                AND UPPER(LTRIM(RTRIM(ISNULL(B.Thumbprint, ''))))
                    = UPPER(LTRIM(RTRIM(ISNULL(C.Thumbprint, ''))))
          )
      )
),
StoredSummary AS
(
    SELECT COUNT(1) AS StoredCertificates
    FROM dbo.ITQS_APP_CertInventory_Current
    WHERE IsPresent = 1
      AND ISNULL(IsActive, 0) = 0
      AND ISNULL(UsageStatus, 'Unknown') IN ('Stored', 'Historical')
)
SELECT
    COUNT(O.CurrentId) AS TotalCertificates,
    COUNT(O.CurrentId) AS ActiveCertificates,
    ISNULL(MAX(SS.StoredCertificates), 0) AS StoredCertificates,

    SUM(CASE WHEN O.IsExpired = 1 THEN 1 ELSE 0 END) AS Expired,
    SUM(CASE WHEN O.IsCritical = 1 THEN 1 ELSE 0 END) AS Critical,
    SUM(CASE WHEN O.IsWarning = 1 THEN 1 ELSE 0 END) AS Warning,
    SUM(CASE WHEN O.IsHealthy = 1 THEN 1 ELSE 0 END) AS Healthy,
    SUM(CASE WHEN O.IsUnknown = 1 THEN 1 ELSE 0 END) AS Unknown,

    SUM
    (
        CASE
            WHEN O.Source LIKE '%ApplicationGateway%'
              OR O.ResourceType LIKE '%applicationGateways%'
            THEN 1 ELSE 0
        END
    ) AS AppGatewayCount,

    SUM
    (
        CASE
            WHEN O.Source LIKE '%AppService%'
              OR O.ResourceType LIKE 'Microsoft.Web/%'
            THEN 1 ELSE 0
        END
    ) AS AppServiceCount,

    SUM
    (
        CASE
            WHEN O.Source LIKE '%KeyVault%'
              OR O.ResourceType LIKE '%KeyVault%'
            THEN 1 ELSE 0
        END
    ) AS KeyVaultCount,

    CONVERT(nvarchar(36), MAX(RS.RunId)) AS LastRunId,
    ISNULL(MAX(RS.ProcessedCustomers), 0) AS ProcessedCustomers,
    ISNULL(MAX(RS.SuccessfulCustomers), 0) AS SuccessfulCustomers,
    ISNULL(MAX(RS.FailedCustomers), 0) AS FailedCustomers,
    MAX(RS.LastScanAt) AS LastScanAt

FROM Operational O
CROSS JOIN StoredSummary SS
LEFT JOIN RunSummary RS
    ON 1 = 1;";

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return new WebCertificateDashboardModel();

        return new WebCertificateDashboardModel
        {
            TotalCertificates = GetInt(reader, "TotalCertificates"),
            ActiveCertificates = GetInt(reader, "ActiveCertificates"),
            StoredCertificates = GetInt(reader, "StoredCertificates"),

            Expired = GetInt(reader, "Expired"),
            Critical = GetInt(reader, "Critical"),
            Warning = GetInt(reader, "Warning"),
            Healthy = GetInt(reader, "Healthy"),
            Unknown = GetInt(reader, "Unknown"),

            AppGatewayCount = GetInt(reader, "AppGatewayCount"),
            AppServiceCount = GetInt(reader, "AppServiceCount"),
            KeyVaultCount = GetInt(reader, "KeyVaultCount"),

            LastRunId = GetString(reader, "LastRunId"),
            ProcessedCustomers = GetInt(reader, "ProcessedCustomers"),
            SuccessfulCustomers = GetInt(reader, "SuccessfulCustomers"),
            FailedCustomers = GetInt(reader, "FailedCustomers"),

            LastScanAt = GetNullableDate(reader, "LastScanAt")
        };
    }

    public async Task<IReadOnlyList<WebCertificateInventoryModel>> GetCertificatesAsync()
    {
        var list = new List<WebCertificateInventoryModel>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;

        cmd.CommandText = @"
SELECT
    C.CurrentId,
    C.CustomerName,
    C.TenantId,
    C.SubscriptionId,
    C.SubscriptionName,
    C.ResourceGroup,
    C.ResourceType,
    C.ResourceName,
    C.CertName,
    C.Source,
    C.HostName,
    C.SslState,
    C.Thumbprint,
    C.Subject,
    C.Issuer,
    C.NotBefore,
    C.NotAfter,
    C.DaysToExpire,
    C.Status,
    C.StatusPriority,
    C.HasExpiryDate,
    C.IsExpired,
    C.IsCritical,
    C.IsWarning,
    C.IsHealthy,
    C.IsUnknown,
    C.IsActive,
    C.UsageStatus,
    C.KeyVaultName,
    C.AppServiceSiteName,
    C.ApplicationGatewayName,
    C.LastScanAt,
    C.IsPresent,
    C.BindingType,
    C.CertificateType,
    C.DomainProvider,
    C.IsManagedCertificate,
    C.UsesKeyVault,
    C.KeyVaultSecretName

FROM dbo.ITQS_APP_CertInventory_Current C

WHERE C.IsPresent = 1
  AND C.IsActive = 1
  AND ISNULL(C.UsageStatus, 'Unknown') = 'Active'

  AND NOT
  (
      C.Source = 'AppServiceCertificateResource'
      AND NULLIF(LTRIM(RTRIM(ISNULL(C.Thumbprint, ''))), '') IS NOT NULL

      AND EXISTS
      (
          SELECT 1
          FROM dbo.ITQS_APP_CertInventory_Current B
          WHERE B.IsPresent = 1
            AND B.IsActive = 1
            AND ISNULL(B.UsageStatus, 'Unknown') = 'Active'
            AND B.Source = 'AppServiceBinding'

            AND ISNULL(B.TenantId, '00000000-0000-0000-0000-000000000000')
                = ISNULL(C.TenantId, '00000000-0000-0000-0000-000000000000')

            AND ISNULL(B.SubscriptionId, '00000000-0000-0000-0000-000000000000')
                = ISNULL(C.SubscriptionId, '00000000-0000-0000-0000-000000000000')

            AND UPPER(LTRIM(RTRIM(ISNULL(B.Thumbprint, ''))))
                = UPPER(LTRIM(RTRIM(ISNULL(C.Thumbprint, ''))))
      )
  )

ORDER BY
    C.StatusPriority ASC,
    CASE
        WHEN C.DaysToExpire IS NULL THEN 999999
        ELSE C.DaysToExpire
    END ASC,
    C.CustomerName ASC,
    C.CertName ASC;";

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new WebCertificateInventoryModel
            {
                CurrentId = GetLong(reader, "CurrentId"),

                CustomerName = GetString(reader, "CustomerName"),
                TenantId = GetString(reader, "TenantId"),
                SubscriptionId = GetString(reader, "SubscriptionId"),
                SubscriptionName = GetString(reader, "SubscriptionName"),

                ResourceGroup = GetString(reader, "ResourceGroup"),
                ResourceType = GetString(reader, "ResourceType"),
                ResourceName = GetString(reader, "ResourceName"),

                CertName = GetString(reader, "CertName"),
                Source = GetString(reader, "Source"),
                HostName = GetString(reader, "HostName"),
                SslState = GetString(reader, "SslState"),

                Thumbprint = GetString(reader, "Thumbprint"),
                Subject = GetString(reader, "Subject"),
                Issuer = GetString(reader, "Issuer"),

                NotBefore = GetNullableDate(reader, "NotBefore"),
                NotAfter = GetNullableDate(reader, "NotAfter"),
                DaysToExpire = GetNullableInt(reader, "DaysToExpire"),

                Status = GetString(reader, "Status"),
                StatusPriority = GetInt(reader, "StatusPriority"),

                HasExpiryDate = GetBool(reader, "HasExpiryDate"),
                IsExpired = GetBool(reader, "IsExpired"),
                IsCritical = GetBool(reader, "IsCritical"),
                IsWarning = GetBool(reader, "IsWarning"),
                IsHealthy = GetBool(reader, "IsHealthy"),
                IsUnknown = GetBool(reader, "IsUnknown"),

                IsActive = GetBool(reader, "IsActive"),
                UsageStatus = GetString(reader, "UsageStatus"),

                KeyVaultName = GetString(reader, "KeyVaultName"),
                AppServiceSiteName = GetString(reader, "AppServiceSiteName"),
                ApplicationGatewayName = GetString(reader, "ApplicationGatewayName"),

                LastScanAt = GetDate(reader, "LastScanAt"),
                IsPresent = GetBool(reader, "IsPresent"),

                BindingType = GetString(reader, "BindingType"),
                CertificateType = GetString(reader, "CertificateType"),
                DomainProvider = GetString(reader, "DomainProvider"),

                IsManagedCertificate = GetBool(reader, "IsManagedCertificate"),
                UsesKeyVault = GetBool(reader, "UsesKeyVault"),
                KeyVaultSecretName = GetString(reader, "KeyVaultSecretName")
            });
        }

        return list;
    }

    private async Task<SqlConnection> OpenConnectionAsync()
    {
        var sqlUser = await _secretClient.GetSecretAsync(
            _keyVaultSettings.SqlUserSecret);

        var sqlPassword = await _secretClient.GetSecretAsync(
            _keyVaultSettings.SqlPasswordSecret);

        var connectionString =
            $"Server=tcp:{_sqlSettings.Server},1433;" +
            $"Initial Catalog={_sqlSettings.Database};" +
            "Persist Security Info=False;" +
            $"User ID={sqlUser.Value.Value};" +
            $"Password={sqlPassword.Value.Value};" +
            "MultipleActiveResultSets=False;" +
            "Encrypt=True;" +
            "TrustServerCertificate=False;" +
            "Connection Timeout=30;";

        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static bool HasColumn(
        SqlDataReader reader,
        string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(
                reader.GetName(i),
                columnName,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetString(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value
            ? string.Empty
            : Convert.ToString(r[name]) ?? string.Empty;

    private static long GetLong(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value
            ? 0L
            : Convert.ToInt64(r[name]);

    private static int GetInt(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value
            ? 0
            : Convert.ToInt32(r[name]);

    private static int? GetNullableInt(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value
            ? null
            : Convert.ToInt32(r[name]);

    private static DateTime GetDate(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value
            ? DateTime.MinValue
            : Convert.ToDateTime(r[name]);

    private static DateTime? GetNullableDate(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value
            ? null
            : Convert.ToDateTime(r[name]);

    private static bool GetBool(SqlDataReader r, string name)
        => HasColumn(r, name)
           && r[name] != DBNull.Value
           && Convert.ToBoolean(r[name]);
}
