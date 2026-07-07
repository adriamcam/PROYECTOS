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
SELECT
    COUNT(1) AS TotalCertificates,
    SUM(CASE WHEN IsExpired = 1 THEN 1 ELSE 0 END) AS Expired,
    SUM(CASE WHEN IsCritical = 1 THEN 1 ELSE 0 END) AS Critical,
    SUM(CASE WHEN IsWarning = 1 THEN 1 ELSE 0 END) AS Warning,
    SUM(CASE WHEN IsHealthy = 1 THEN 1 ELSE 0 END) AS Healthy,
    SUM(CASE WHEN IsUnknown = 1 THEN 1 ELSE 0 END) AS Unknown,
    SUM(CASE WHEN Source LIKE '%ApplicationGateway%' OR ResourceType LIKE '%applicationGateways%' THEN 1 ELSE 0 END) AS AppGatewayCount,
    SUM(CASE WHEN Source LIKE '%AppService%' OR ResourceType LIKE 'Microsoft.Web/%' THEN 1 ELSE 0 END) AS AppServiceCount,
    SUM(CASE WHEN Source LIKE '%KeyVault%' OR ResourceType LIKE '%KeyVault%' THEN 1 ELSE 0 END) AS KeyVaultCount,
    MAX(LastScanAt) AS LastScanAt
FROM dbo.ITQS_APP_CertInventory_Current
WHERE IsPresent = 1;";

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return new WebCertificateDashboardModel();

        return new WebCertificateDashboardModel
        {
            TotalCertificates = GetInt(reader, "TotalCertificates"),
            Expired = GetInt(reader, "Expired"),
            Critical = GetInt(reader, "Critical"),
            Warning = GetInt(reader, "Warning"),
            Healthy = GetInt(reader, "Healthy"),
            Unknown = GetInt(reader, "Unknown"),
            AppGatewayCount = GetInt(reader, "AppGatewayCount"),
            AppServiceCount = GetInt(reader, "AppServiceCount"),
            KeyVaultCount = GetInt(reader, "KeyVaultCount"),
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
    CurrentId,
    CustomerName,
    TenantId,
    SubscriptionId,
    SubscriptionName,
    ResourceGroup,
    ResourceType,
    ResourceName,
    CertName,
    Source,
    HostName,
    SslState,
    Thumbprint,
    Subject,
    Issuer,
    NotBefore,
    NotAfter,
    DaysToExpire,
    Status,
    StatusPriority,
    HasExpiryDate,
    IsExpired,
    IsCritical,
    IsWarning,
    IsHealthy,
    IsUnknown,
    KeyVaultName,
    AppServiceSiteName,
    ApplicationGatewayName,
    LastScanAt,
    IsPresent,
    BindingType,
    CertificateType,
    DomainProvider,
    IsManagedCertificate,
    UsesKeyVault,
    KeyVaultSecretName
FROM dbo.ITQS_APP_CertInventory_Current
WHERE IsPresent = 1
ORDER BY
    StatusPriority ASC,
    CASE WHEN DaysToExpire IS NULL THEN 999999 ELSE DaysToExpire END ASC,
    CustomerName ASC,
    CertName ASC;";

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
        var sqlUser = await _secretClient.GetSecretAsync(_keyVaultSettings.SqlUserSecret);
        var sqlPassword = await _secretClient.GetSecretAsync(_keyVaultSettings.SqlPasswordSecret);

        var connectionString =
            $"Server=tcp:{_sqlSettings.Server},1433;" +
            $"Initial Catalog={_sqlSettings.Database};" +
            $"Persist Security Info=False;" +
            $"User ID={sqlUser.Value.Value};" +
            $"Password={sqlPassword.Value.Value};" +
            "MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string GetString(SqlDataReader r, string name) => !HasColumn(r, name) || r[name] == DBNull.Value ? string.Empty : Convert.ToString(r[name]) ?? string.Empty;
    private static long GetLong(SqlDataReader r, string name) => !HasColumn(r, name) || r[name] == DBNull.Value ? 0L : Convert.ToInt64(r[name]);
    private static int GetInt(SqlDataReader r, string name) => !HasColumn(r, name) || r[name] == DBNull.Value ? 0 : Convert.ToInt32(r[name]);
    private static int? GetNullableInt(SqlDataReader r, string name) => !HasColumn(r, name) || r[name] == DBNull.Value ? null : Convert.ToInt32(r[name]);
    private static DateTime GetDate(SqlDataReader r, string name) => !HasColumn(r, name) || r[name] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r[name]);
    private static DateTime? GetNullableDate(SqlDataReader r, string name) => !HasColumn(r, name) || r[name] == DBNull.Value ? null : Convert.ToDateTime(r[name]);
    private static bool GetBool(SqlDataReader r, string name) => HasColumn(r, name) && r[name] != DBNull.Value && Convert.ToBoolean(r[name]);
}