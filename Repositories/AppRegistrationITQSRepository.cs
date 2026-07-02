using System.Data;
using Microsoft.Data.SqlClient;
using Azure.Security.KeyVault.Secrets;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Administration.AppRegistrations;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class AppRegistrationITQSRepository : IAppRegistrationITQSRepository
{
    private readonly SqlSettings _sqlSettings;
    private readonly KeyVaultSettings _keyVaultSettings;
    private readonly SecretClient _secretClient;

    public AppRegistrationITQSRepository(
        IOptions<SqlSettings> sqlOptions,
        IOptions<KeyVaultSettings> keyVaultOptions,
        SecretClient secretClient)
    {
        _sqlSettings = sqlOptions.Value;
        _keyVaultSettings = keyVaultOptions.Value;
        _secretClient = secretClient;
    }

    public async Task<AppRegistrationDashboardModel> GetDashboardAsync()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = CreateStoredProcedure(conn, "dbo.sp_AppRegistrationsITQS_Dashboard");

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return new AppRegistrationDashboardModel();

        return new AppRegistrationDashboardModel
        {
            TotalCustomers = GetInt(reader, "TotalCustomers"),
            TotalAppRegistrations = GetInt(reader, "TotalAppRegistrations"),
            TotalSecrets = GetInt(reader, "TotalSecrets"),
            TotalCertificates = GetInt(reader, "TotalCertificates"),
            Healthy = GetInt(reader, "Healthy"),
            ExpireIn30Days = GetInt(reader, "ExpireIn30Days"),
            ExpireIn15Days = GetInt(reader, "ExpireIn15Days"),
            Expired = GetInt(reader, "Expired"),
            LastScanDate = GetNullableDate(reader, "LastScanDate")
        };
    }

    public async Task<IReadOnlyList<AppRegistrationListItem>> GetListAsync(AppRegistrationFilterModel filters)
    {
        var list = new List<AppRegistrationListItem>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = CreateStoredProcedure(conn, "dbo.sp_AppRegistrationsITQS_List");

        AddParam(cmd, "@Search", filters.Search);
        AddParam(cmd, "@CustomerName", filters.CustomerName);
        AddParam(cmd, "@CredentialType", filters.CredentialType);
        AddParam(cmd, "@RiskLevel", filters.RiskLevel);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(MapListItem(reader));
        }

        return list;
    }

    public async Task<AppRegistrationDetailModel?> GetDetailAsync(long id)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = CreateStoredProcedure(conn, "dbo.sp_AppRegistrationsITQS_Detail");
        AddParam(cmd, "@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        var item = MapListItem(reader);

        return new AppRegistrationDetailModel
        {
            Id = item.Id,
            CustomerName = item.CustomerName,
            TenantId = item.TenantId,
            SubscriptionId = item.SubscriptionId,
            SubscriptionName = item.SubscriptionName,
            AppName = item.AppName,
            ClientId = item.ClientId,
            CredentialType = item.CredentialType,
            KeyId = item.KeyId,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            DaysToExpire = item.DaysToExpire,
            IsExpired = item.IsExpired,
            ScanDate = item.ScanDate,
            RiskLevel = item.RiskLevel,
            HealthPercent = item.HealthPercent
        };
    }

    public async Task<IReadOnlyList<AppRegistrationAssignableUserModel>> GetAssignableUsersAsync()
    {
        var list = new List<AppRegistrationAssignableUserModel>();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = CreateStoredProcedure(conn, "dbo.sp_AppRegistrationsITQS_AssignableUsers_List");

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new AppRegistrationAssignableUserModel
            {
                UserId = GetInt(reader, "UserId"),
                DisplayName = GetString(reader, "DisplayName"),
                Email = GetString(reader, "Email"),
                BaseRole = GetString(reader, "BaseRole"),
                EffectiveRole = GetString(reader, "EffectiveRole")
            });
        }

        return list;
    }

    public async Task<long> CreateTaskAsync(AppRegistrationAssignRequest request)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = CreateStoredProcedure(conn, "dbo.sp_AppRegistrationsITQS_Task_Create");

        AddParam(cmd, "@AppRegistrationId", request.AppRegistrationId);
        AddParam(cmd, "@CustomerName", request.CustomerName);
        AddParam(cmd, "@TenantId", request.TenantId);
        AddParam(cmd, "@SubscriptionId", request.SubscriptionId);
        AddParam(cmd, "@SubscriptionName", request.SubscriptionName);
        AddParam(cmd, "@AppName", request.AppName);
        AddParam(cmd, "@ClientId", request.ClientId);
        AddParam(cmd, "@CredentialType", request.CredentialType);
        AddParam(cmd, "@KeyId", request.KeyId);
        AddParam(cmd, "@EndDate", request.EndDate);
        AddParam(cmd, "@DaysToExpire", request.DaysToExpire);
        AddParam(cmd, "@Priority", request.Priority);
        AddParam(cmd, "@AssignedTo", request.AssignedTo);
        AddParam(cmd, "@AssignedEmail", request.AssignedEmail);
        AddParam(cmd, "@AssignedBy", request.AssignedBy);
        AddParam(cmd, "@RequiredDate", request.RequiredDate?.Date);
        AddParam(cmd, "@Notes", request.Notes);

        var result = await cmd.ExecuteScalarAsync();

        return Convert.ToInt64(result);
    }

    public async Task MarkTaskEmailSentAsync(long taskId)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = CreateStoredProcedure(conn, "dbo.sp_AppRegistrationsITQS_Task_MarkEmailSent");
        AddParam(cmd, "@TaskId", taskId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<SqlConnection> OpenConnectionAsync()
    {
        var sqlUser = await _secretClient.GetSecretAsync(_keyVaultSettings.SqlUserSecret);
        var sqlPassword = await _secretClient.GetSecretAsync(_keyVaultSettings.SqlPasswordSecret);

        var connectionString =
            $"Server=tcp:{_sqlSettings.Server},1433;" +
            $"Initial Catalog={_sqlSettings.Database};" +
            $"User ID={sqlUser.Value.Value};" +
            $"Password={sqlPassword.Value.Value};" +
            "Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static SqlCommand CreateStoredProcedure(SqlConnection conn, string name)
    {
        return new SqlCommand(name, conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };
    }

    private static void AddParam(SqlCommand cmd, string name, object? value)
    {
        cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static AppRegistrationListItem MapListItem(SqlDataReader reader)
    {
        return new AppRegistrationListItem
        {
            Id = GetLong(reader, "Id"),
            CustomerName = GetString(reader, "CustomerName"),
            TenantId = GetGuid(reader, "TenantId"),
            SubscriptionId = GetNullableGuid(reader, "SubscriptionId"),
            SubscriptionName = GetString(reader, "SubscriptionName"),
            AppName = GetString(reader, "AppName"),
            ClientId = GetGuid(reader, "ClientId"),
            CredentialType = GetString(reader, "CredentialType"),
            KeyId = GetNullableGuid(reader, "KeyId"),
            StartDate = GetNullableDate(reader, "StartDate"),
            EndDate = GetNullableDate(reader, "EndDate"),
            DaysToExpire = GetNullableInt(reader, "DaysToExpire"),
            IsExpired = GetBool(reader, "IsExpired"),
            ScanDate = GetDate(reader, "ScanDate"),
            RiskLevel = GetString(reader, "RiskLevel"),
            HealthPercent = GetInt(reader, "HealthPercent")
        };
    }

    private static string GetString(SqlDataReader r, string name) => r[name] == DBNull.Value ? string.Empty : Convert.ToString(r[name]) ?? string.Empty;
    private static int GetInt(SqlDataReader r, string name) => r[name] == DBNull.Value ? 0 : Convert.ToInt32(r[name]);
    private static int? GetNullableInt(SqlDataReader r, string name) => r[name] == DBNull.Value ? null : Convert.ToInt32(r[name]);
    private static long GetLong(SqlDataReader r, string name) => r[name] == DBNull.Value ? 0 : Convert.ToInt64(r[name]);
    private static Guid GetGuid(SqlDataReader r, string name) => r[name] == DBNull.Value ? Guid.Empty : (Guid)r[name];
    private static Guid? GetNullableGuid(SqlDataReader r, string name) => r[name] == DBNull.Value ? null : (Guid)r[name];
    private static DateTime GetDate(SqlDataReader r, string name) => r[name] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r[name]);
    private static DateTime? GetNullableDate(SqlDataReader r, string name) => r[name] == DBNull.Value ? null : Convert.ToDateTime(r[name]);
    private static bool GetBool(SqlDataReader r, string name) => r[name] != DBNull.Value && Convert.ToBoolean(r[name]);
}

