using System.Data;
using Azure.Security.KeyVault.Secrets;
using ITQS.SupportOperationsCenter.Data;
using ITQS.SupportOperationsCenter.Models.Administration.GdapAdminLinks;
using ITQS.SupportOperationsCenter.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ITQS.SupportOperationsCenter.Repositories;

public sealed class GdapAdminLinksRepository : IGdapAdminLinksRepository
{
    private readonly SqlSettings _sqlSettings;
    private readonly KeyVaultSettings _keyVaultSettings;
    private readonly SecretClient _secretClient;

    public GdapAdminLinksRepository(
        IOptions<SqlSettings> sqlOptions,
        IOptions<KeyVaultSettings> keyVaultOptions,
        SecretClient secretClient)
    {
        _sqlSettings = sqlOptions.Value;
        _keyVaultSettings = keyVaultOptions.Value;
        _secretClient = secretClient;
    }

    public async Task<GdapAdminLinksDashboardModel> GetDashboardAsync()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
SELECT
    TotalCustomers = COUNT(1),
    ActiveGdap = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND LOWER(ISNULL(StatusFound,'')) LIKE '%active%' THEN 1 ELSE 0 END),
    WithoutGdap = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND (LOWER(ISNULL(StatusFound,'')) LIKE '%sin gdap%' OR LOWER(ISNULL(HasGdap,'')) IN ('no','false','0')) THEN 1 ELSE 0 END),
    ApprovalPending = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND LOWER(ISNULL(StatusFound,'')) LIKE '%approvalpending%' THEN 1 ELSE 0 END),
    ExpiringIn30Days = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND ActiveEndDate IS NOT NULL AND DATEDIFF(DAY, GETDATE(), ActiveEndDate) BETWEEN 0 AND 30 THEN 1 ELSE 0 END),
    ExpiringIn15Days = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND ActiveEndDate IS NOT NULL AND DATEDIFF(DAY, GETDATE(), ActiveEndDate) BETWEEN 0 AND 15 THEN 1 ELSE 0 END),
    ExpiringIn5Days = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND ActiveEndDate IS NOT NULL AND DATEDIFF(DAY, GETDATE(), ActiveEndDate) BETWEEN 0 AND 5 THEN 1 ELSE 0 END),
    DisabledCustomers = SUM(CASE WHEN ISNULL(IsActive,1) = 0 THEN 1 ELSE 0 END),
    PendingEmails = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND LOWER(ISNULL(StatusFound,'')) LIKE '%approvalpending%' AND ISNULL(ApprovalPendingLink,'') <> '' AND ISNULL(PrimaryEmail,'') <> '' THEN 1 ELSE 0 END),
    AutomationErrors = SUM(CASE WHEN LOWER(ISNULL(LastAutomationStatus,'')) LIKE '%fail%' OR LOWER(ISNULL(LastAutomationStatus,'')) LIKE '%error%' THEN 1 ELSE 0 END),
    LastExecutionDate = MAX(ExecutionDate),
    LastUpdated = MAX(LastUpdated)
FROM dbo.vw_GDAP_AllCustomers;";

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return new GdapAdminLinksDashboardModel();

        return new GdapAdminLinksDashboardModel
        {
            TotalCustomers = GetInt(reader, "TotalCustomers"),
            ActiveGdap = GetInt(reader, "ActiveGdap"),
            WithoutGdap = GetInt(reader, "WithoutGdap"),
            ApprovalPending = GetInt(reader, "ApprovalPending"),
            ExpiringIn30Days = GetInt(reader, "ExpiringIn30Days"),
            ExpiringIn15Days = GetInt(reader, "ExpiringIn15Days"),
            ExpiringIn5Days = GetInt(reader, "ExpiringIn5Days"),
            DisabledCustomers = GetInt(reader, "DisabledCustomers"),
            PendingEmails = GetInt(reader, "PendingEmails"),
            AutomationErrors = GetInt(reader, "AutomationErrors"),
            LastExecutionDate = GetNullableDate(reader, "LastExecutionDate"),
            LastUpdated = GetNullableDate(reader, "LastUpdated")
        };
    }

    public async Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetCustomersAsync(GdapAdminLinksFilterModel filters)
    {
        var list = new List<GdapAdminLinksCustomerModel>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
SELECT TOP (1000)
    *,
    DaysToExpire = CASE WHEN ActiveEndDate IS NULL THEN NULL ELSE DATEDIFF(DAY, GETDATE(), ActiveEndDate) END
FROM dbo.vw_GDAP_AllCustomers
WHERE
    (@Search IS NULL OR @Search = '' OR CustomerName LIKE '%' + @Search + '%' OR CustomerTenantId LIKE '%' + @Search + '%' OR PartnerTenant LIKE '%' + @Search + '%')
    AND (@PartnerTenant IS NULL OR @PartnerTenant = '' OR PartnerTenant = @PartnerTenant)
    AND (@StatusFound IS NULL OR @StatusFound = '' OR StatusFound LIKE '%' + @StatusFound + '%')
    AND (@ActiveFilter IS NULL OR @ActiveFilter = '' OR (@ActiveFilter = 'Active' AND ISNULL(IsActive,1) = 1) OR (@ActiveFilter = 'Inactive' AND ISNULL(IsActive,1) = 0))
    AND (@EmailFilter IS NULL OR @EmailFilter = '' OR (@EmailFilter = 'Ready' AND ISNULL(PrimaryEmail,'') <> '' AND ISNULL(ApprovalPendingLink,'') <> '') OR (@EmailFilter = 'Missing' AND ISNULL(PrimaryEmail,'') = ''))
ORDER BY
    CASE WHEN LOWER(ISNULL(StatusFound,'')) LIKE '%approvalpending%' THEN 0 ELSE 1 END,
    CASE WHEN ActiveEndDate IS NULL THEN 1 ELSE 0 END,
    ActiveEndDate ASC,
    CustomerName ASC;";

        AddParam(cmd, "@Search", filters.Search);
        AddParam(cmd, "@PartnerTenant", filters.PartnerTenant);
        AddParam(cmd, "@StatusFound", filters.StatusFound);
        AddParam(cmd, "@ActiveFilter", filters.ActiveFilter);
        AddParam(cmd, "@EmailFilter", filters.EmailFilter);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapCustomer(reader));

        return list;
    }

    public async Task<GdapAdminLinksCustomerModel?> GetCustomerAsync(int id)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
SELECT TOP (1)
    *,
    DaysToExpire = CASE WHEN ActiveEndDate IS NULL THEN NULL ELSE DATEDIFF(DAY, GETDATE(), ActiveEndDate) END
FROM dbo.vw_GDAP_AllCustomers
WHERE Id = @Id;";
        AddParam(cmd, "@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapCustomer(reader) : null;
    }

    public async Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetPendingEmailsAsync()
    {
        var list = new List<GdapAdminLinksCustomerModel>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
SELECT *, DaysToExpire = CASE WHEN ActiveEndDate IS NULL THEN NULL ELSE DATEDIFF(DAY, GETDATE(), ActiveEndDate) END
FROM dbo.vw_GDAP_PendingEmails
ORDER BY CustomerName;";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapCustomer(reader));

        return list;
    }

    public async Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetExpiringSoonAsync()
    {
        var list = new List<GdapAdminLinksCustomerModel>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
SELECT *, DaysToExpire = CASE WHEN ActiveEndDate IS NULL THEN NULL ELSE DATEDIFF(DAY, GETDATE(), ActiveEndDate) END
FROM dbo.vw_GDAP_ExpiringSoon
ORDER BY ActiveEndDate ASC, CustomerName ASC;";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapCustomer(reader));

        return list;
    }

    public async Task UpdateCustomerAsync(GdapAdminLinksSaveCustomerRequest request)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
UPDATE dbo.PartnerCenterCustomers
SET
    PrimaryContactName = @PrimaryContactName,
    PrimaryEmail = @PrimaryEmail,
    CCEmails = @CCEmails,
    AutoSendEmail = @AutoSendEmail,
    IsActive = @IsActive,
    ExcludeReason = NULLIF(@ExcludeReason,''),
    UpdatedBy = @UpdatedBy,
    LastUpdated = GETDATE()
WHERE Id = @Id;";

        AddParam(cmd, "@Id", request.Id);
        AddParam(cmd, "@PrimaryContactName", request.PrimaryContactName);
        AddParam(cmd, "@PrimaryEmail", request.PrimaryEmail);
        AddParam(cmd, "@CCEmails", request.CCEmails);
        AddParam(cmd, "@AutoSendEmail", request.AutoSendEmail);
        AddParam(cmd, "@IsActive", request.IsActive);
        AddParam(cmd, "@ExcludeReason", request.ExcludeReason);
        AddParam(cmd, "@UpdatedBy", request.UpdatedBy);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SetCustomerActiveAsync(int id, bool isActive, string updatedBy, string reason)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
UPDATE dbo.PartnerCenterCustomers
SET
    IsActive = @IsActive,
    ExcludeReason = CASE WHEN @IsActive = 1 THEN NULL ELSE NULLIF(@Reason,'') END,
    UpdatedBy = @UpdatedBy,
    LastUpdated = GETDATE()
WHERE Id = @Id;";

        AddParam(cmd, "@Id", id);
        AddParam(cmd, "@IsActive", isActive);
        AddParam(cmd, "@UpdatedBy", updatedBy);
        AddParam(cmd, "@Reason", reason);

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

    private static void AddParam(SqlCommand cmd, string name, object? value)
        => cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static GdapAdminLinksCustomerModel MapCustomer(SqlDataReader reader)
    {
        return new GdapAdminLinksCustomerModel
        {
            Id = GetInt(reader, "Id"),
            ExecutionDate = GetDate(reader, "ExecutionDate"),
            PartnerTenant = GetString(reader, "PartnerTenant"),
            CustomerName = GetString(reader, "CustomerName"),
            CustomerTenantId = GetString(reader, "CustomerTenantId"),
            HasGdap = GetString(reader, "HasGdap"),
            RelationshipQty = GetInt(reader, "RelationshipQty"),
            StatusFound = GetString(reader, "StatusFound"),
            ActiveEndDate = GetNullableDate(reader, "ActiveEndDate"),
            LastUpdated = GetNullableDate(reader, "LastUpdated"),
            ApprovalPendingLink = GetString(reader, "ApprovalPendingLink"),
            IsActive = GetBool(reader, "IsActive", true),
            PrimaryContactName = GetString(reader, "PrimaryContactName"),
            PrimaryEmail = GetString(reader, "PrimaryEmail"),
            CCEmails = GetString(reader, "CCEmails"),
            AutoSendEmail = GetBool(reader, "AutoSendEmail", false),
            ExcludeReason = GetString(reader, "ExcludeReason"),
            LastEmailSentAt = GetNullableDate(reader, "LastEmailSentAt"),
            LastEmailSentBy = GetString(reader, "LastEmailSentBy"),
            SendMailStatus = GetString(reader, "SendMailStatus"),
            SendMailAttempts = GetInt(reader, "SendMailAttempts"),
            LastAutomationStatus = GetString(reader, "LastAutomationStatus"),
            LastAutomationMessage = GetString(reader, "LastAutomationMessage"),
            UpdatedBy = GetString(reader, "UpdatedBy"),
            DaysToExpire = GetNullableInt(reader, "DaysToExpire")
        };
    }

    private static bool HasColumn(SqlDataReader r, string name)
    {
        for (var i = 0; i < r.FieldCount; i++)
        {
            if (r.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string GetString(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value ? string.Empty : Convert.ToString(r[name]) ?? string.Empty;

    private static int GetInt(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value ? 0 : Convert.ToInt32(r[name]);

    private static int? GetNullableInt(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value ? null : Convert.ToInt32(r[name]);

    private static DateTime GetDate(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r[name]);

    private static DateTime? GetNullableDate(SqlDataReader r, string name)
        => !HasColumn(r, name) || r[name] == DBNull.Value ? null : Convert.ToDateTime(r[name]);

    private static bool GetBool(SqlDataReader r, string name, bool defaultValue)
        => !HasColumn(r, name) || r[name] == DBNull.Value ? defaultValue : Convert.ToBoolean(r[name]);
}
