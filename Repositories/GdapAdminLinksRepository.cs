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
    ExpiringIn7Days = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND ActiveEndDate IS NOT NULL AND DATEDIFF(DAY, GETDATE(), ActiveEndDate) BETWEEN 0 AND 7 THEN 1 ELSE 0 END),
    ExpiringIn5Days = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND ActiveEndDate IS NOT NULL AND DATEDIFF(DAY, GETDATE(), ActiveEndDate) BETWEEN 0 AND 5 THEN 1 ELSE 0 END),
    DisabledCustomers = SUM(CASE WHEN ISNULL(IsActive,1) = 0 THEN 1 ELSE 0 END),
    PendingEmails = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND LOWER(ISNULL(StatusFound,'')) LIKE '%approvalpending%' AND ISNULL(ApprovalPendingLink,'') <> '' AND ISNULL(PrimaryEmail,'') <> '' THEN 1 ELSE 0 END),
    AutomationErrors = SUM(CASE WHEN LOWER(ISNULL(LastAutomationStatus,'')) IN ('failed','error') THEN 1 ELSE 0 END),
    LastExecutionDate = MAX(ExecutionDate),
    LastUpdated = MAX(LastUpdated)
FROM dbo.PartnerCenterCustomers;";

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
            ExpiringIn7Days = GetInt(reader, "ExpiringIn7Days"),
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


    public async Task<IReadOnlyList<GdapAdminLinksCustomerModel>> GetExpirationEmailQueueAsync(int daysToExpire)
    {
        if (daysToExpire != 7 && daysToExpire != 15 && daysToExpire != 30)
            throw new InvalidOperationException("Solo se permiten recordatorios de 7, 15 o 30 días.");

        var list = new List<GdapAdminLinksCustomerModel>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
SELECT
    *,
    DaysToExpire = CASE WHEN ActiveEndDate IS NULL THEN NULL ELSE DATEDIFF(DAY, GETDATE(), ActiveEndDate) END
FROM dbo.vw_GDAP_AllCustomers
WHERE
    ISNULL(IsActive,1) = 1
    AND ActiveEndDate IS NOT NULL
    AND DATEDIFF(DAY, GETDATE(), ActiveEndDate) BETWEEN 0 AND @DaysToExpire
    AND LOWER(ISNULL(StatusFound,'')) LIKE '%approvalpending%'
    AND ISNULL(ApprovalPendingLink,'') <> ''
    AND ISNULL(PrimaryEmail,'') <> ''
    AND NOT EXISTS
    (
        SELECT 1
        FROM dbo.PartnerCenterCustomerHistory h
        WHERE h.CustomerTenantId = vw_GDAP_AllCustomers.CustomerTenantId
          AND h.EventType = CONCAT('Correo GDAP ', @DaysToExpire, ' días')
          AND CONVERT(date, h.EventDate) = CONVERT(date, GETDATE())
    )
ORDER BY ActiveEndDate ASC, CustomerName ASC;";
        AddParam(cmd, "@DaysToExpire", daysToExpire);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapCustomer(reader));

        return list;
    }


    public async Task<IReadOnlyList<GdapAdminLinksAuditEventModel>> GetAuditEventsAsync(int? customerId = null)
    {
        var list = new List<GdapAdminLinksAuditEventModel>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
SELECT TOP (500)
    h.Id,
    CustomerId = ISNULL(c.Id, 0),
    h.CustomerTenantId,
    h.PartnerTenant,
    h.CustomerName,
    h.EventDate,
    h.EventType,
    h.Description,
    h.ExecutedBy,
    h.ApprovalUrl
FROM dbo.PartnerCenterCustomerHistory h
LEFT JOIN dbo.PartnerCenterCustomers c
    ON c.CustomerTenantId = h.CustomerTenantId
WHERE (@CustomerId IS NULL OR c.Id = @CustomerId)
ORDER BY h.EventDate DESC, h.Id DESC;";
        AddParam(cmd, "@CustomerId", customerId);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new GdapAdminLinksAuditEventModel
            {
                Id = GetLong(reader, "Id"),
                CustomerId = GetInt(reader, "CustomerId"),
                CustomerTenantId = GetString(reader, "CustomerTenantId"),
                PartnerTenant = GetString(reader, "PartnerTenant"),
                CustomerName = GetString(reader, "CustomerName"),
                EventDate = GetDate(reader, "EventDate"),
                EventType = GetString(reader, "EventType"),
                Description = GetString(reader, "Description"),
                ExecutedBy = GetString(reader, "ExecutedBy"),
                ApprovalUrl = GetString(reader, "ApprovalUrl")
            });
        }
        return list;
    }

    public async Task<IReadOnlyList<GdapAdminLinksReportModel>> GetReportByPartnerAsync()
    {
        var list = new List<GdapAdminLinksReportModel>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
SELECT
    PartnerTenant = ISNULL(PartnerTenant, 'Sin Partner'),
    TotalCustomers = COUNT(1),
    ActiveGdap = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND LOWER(ISNULL(StatusFound,'')) LIKE '%active%' THEN 1 ELSE 0 END),
    ApprovalPending = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND LOWER(ISNULL(StatusFound,'')) LIKE '%approvalpending%' THEN 1 ELSE 0 END),
    WithoutGdap = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND (LOWER(ISNULL(StatusFound,'')) LIKE '%sin gdap%' OR LOWER(ISNULL(HasGdap,'')) IN ('no','false','0')) THEN 1 ELSE 0 END),
    ExpiringIn30Days = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND ActiveEndDate IS NOT NULL AND DATEDIFF(DAY, GETDATE(), ActiveEndDate) BETWEEN 0 AND 30 THEN 1 ELSE 0 END),
    DisabledCustomers = SUM(CASE WHEN ISNULL(IsActive,1) = 0 THEN 1 ELSE 0 END),
    PendingEmails = SUM(CASE WHEN ISNULL(IsActive,1) = 1 AND LOWER(ISNULL(StatusFound,'')) LIKE '%approvalpending%' AND ISNULL(ApprovalPendingLink,'') <> '' AND ISNULL(PrimaryEmail,'') <> '' THEN 1 ELSE 0 END),
    AutomationErrors = SUM(CASE WHEN LOWER(ISNULL(LastAutomationStatus,'')) IN ('failed','error') THEN 1 ELSE 0 END)
FROM dbo.vw_GDAP_AllCustomers
GROUP BY ISNULL(PartnerTenant, 'Sin Partner')
ORDER BY PartnerTenant;";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new GdapAdminLinksReportModel
            {
                PartnerTenant = GetString(reader, "PartnerTenant"),
                TotalCustomers = GetInt(reader, "TotalCustomers"),
                ActiveGdap = GetInt(reader, "ActiveGdap"),
                ApprovalPending = GetInt(reader, "ApprovalPending"),
                WithoutGdap = GetInt(reader, "WithoutGdap"),
                ExpiringIn30Days = GetInt(reader, "ExpiringIn30Days"),
                DisabledCustomers = GetInt(reader, "DisabledCustomers"),
                PendingEmails = GetInt(reader, "PendingEmails"),
                AutomationErrors = GetInt(reader, "AutomationErrors")
            });
        }
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

    public async Task RegisterHistoryAsync(int id, string eventType, string description, string executedBy, string? approvalUrl = null)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 120;
        cmd.CommandText = @"
INSERT INTO dbo.PartnerCenterCustomerHistory
(
    CustomerTenantId,
    PartnerTenant,
    CustomerName,
    EventDate,
    EventType,
    Description,
    ExecutedBy,
    ApprovalUrl
)
SELECT
    CustomerTenantId,
    PartnerTenant,
    CustomerName,
    GETDATE(),
    @EventType,
    @Description,
    @ExecutedBy,
    COALESCE(NULLIF(@ApprovalUrl,''), ApprovalPendingLink)
FROM dbo.PartnerCenterCustomers
WHERE Id = @Id;";

        AddParam(cmd, "@Id", id);
        AddParam(cmd, "@EventType", eventType);
        AddParam(cmd, "@Description", description);
        AddParam(cmd, "@ExecutedBy", executedBy);
        AddParam(cmd, "@ApprovalUrl", approvalUrl ?? string.Empty);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkAutomationStartedAsync(GdapAdminLinksAutomationRequest request, string jobId)
    {
        await using var conn = await OpenConnectionAsync();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

        try
        {
            await using (var updateCmd = conn.CreateCommand())
            {
                updateCmd.Transaction = tx;
                updateCmd.CommandType = CommandType.Text;
                updateCmd.CommandTimeout = 120;
                updateCmd.CommandText = @"
UPDATE dbo.PartnerCenterCustomers
SET
    LastAutomationStatus = 'Starting',
    LastAutomationMessage = CONCAT('Automation solicitada por ', @RequestedBy),
    UpdatedBy = @RequestedBy,
    LastUpdated = GETDATE()
WHERE Id = @CustomerId;";
                AddParam(updateCmd, "@CustomerId", request.CustomerId);
                AddParam(updateCmd, "@RequestedBy", request.RequestedBy);
                await updateCmd.ExecuteNonQueryAsync();
            }

            await using (var historyCmd = conn.CreateCommand())
            {
                historyCmd.Transaction = tx;
                historyCmd.CommandType = CommandType.Text;
                historyCmd.CommandTimeout = 120;
                historyCmd.CommandText = @"
INSERT INTO dbo.PartnerCenterCustomerHistory
(CustomerTenantId, PartnerTenant, CustomerName, EventDate, EventType, Description, ExecutedBy, ApprovalUrl)
VALUES
(@CustomerTenantId, @PartnerTenant, @CustomerName, GETDATE(), 'Automation solicitada', CONCAT('Se solicitó ejecución individual de ', @RunbookName, '. JobId inicial: ', @JobId), @RequestedBy, NULL);";
                AddParam(historyCmd, "@CustomerTenantId", request.CustomerTenantId);
                AddParam(historyCmd, "@PartnerTenant", request.PartnerTenant);
                AddParam(historyCmd, "@CustomerName", request.CustomerName);
                AddParam(historyCmd, "@RequestedBy", request.RequestedBy);
                AddParam(historyCmd, "@JobId", jobId);
                AddParam(historyCmd, "@RunbookName", "ITQS-SOC-GENERA-GDAP-PC-CLIENTES");
                await historyCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task MarkAutomationFinishedAsync(GdapAdminLinksAutomationRequest request, GdapAdminLinksAutomationResult result)
    {
        await using var conn = await OpenConnectionAsync();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

        try
        {
            await using (var updateCmd = conn.CreateCommand())
            {
                updateCmd.Transaction = tx;
                updateCmd.CommandType = CommandType.Text;
                updateCmd.CommandTimeout = 120;
                updateCmd.CommandText = @"
UPDATE dbo.PartnerCenterCustomers
SET
    LastAutomationStatus = @Status,
    LastAutomationMessage = @Message,
    UpdatedBy = @RequestedBy,
    LastUpdated = GETDATE()
WHERE Id = @CustomerId;";
                AddParam(updateCmd, "@CustomerId", request.CustomerId);
                AddParam(updateCmd, "@Status", result.Success ? "Started" : "Failed");
                AddParam(updateCmd, "@Message", result.Success ? result.Message : result.ErrorMessage);
                AddParam(updateCmd, "@RequestedBy", request.RequestedBy);
                await updateCmd.ExecuteNonQueryAsync();
            }

            await using (var historyCmd = conn.CreateCommand())
            {
                historyCmd.Transaction = tx;
                historyCmd.CommandType = CommandType.Text;
                historyCmd.CommandTimeout = 120;
                historyCmd.CommandText = @"
INSERT INTO dbo.PartnerCenterCustomerHistory
(CustomerTenantId, PartnerTenant, CustomerName, EventDate, EventType, Description, ExecutedBy, ApprovalUrl)
VALUES
(@CustomerTenantId, @PartnerTenant, @CustomerName, GETDATE(), @EventType, @Description, @RequestedBy, NULL);";
                AddParam(historyCmd, "@CustomerTenantId", request.CustomerTenantId);
                AddParam(historyCmd, "@PartnerTenant", request.PartnerTenant);
                AddParam(historyCmd, "@CustomerName", request.CustomerName);
                AddParam(historyCmd, "@EventType", result.Success ? "Automation iniciada" : "Automation error");
                AddParam(historyCmd, "@Description", result.Success ? result.Message : result.ErrorMessage);
                AddParam(historyCmd, "@RequestedBy", request.RequestedBy);
                await historyCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }



    public async Task<IReadOnlyList<GdapMailTemplateModel>> GetMailTemplatesAsync()
    {
        var list = new List<GdapMailTemplateModel>();
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_GDAP_MailTemplates_List";
        cmd.CommandTimeout = 120;

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapMailTemplate(reader));

        return list;
    }

    public async Task<GdapMailTemplateModel?> GetMailTemplateAsync(int id)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_GDAP_MailTemplates_Get";
        cmd.CommandTimeout = 120;
        AddParam(cmd, "@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapMailTemplate(reader) : null;
    }

    public async Task<GdapMailTemplateModel?> GetDefaultMailTemplateAsync()
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_GDAP_MailTemplates_GetDefault";
        cmd.CommandTimeout = 120;

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapMailTemplate(reader) : null;
    }

    public async Task<int> SaveMailTemplateAsync(GdapMailTemplateModel template)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_GDAP_MailTemplates_Save";
        cmd.CommandTimeout = 120;

        AddParam(cmd, "@Id", template.Id == 0 ? null : template.Id);
        AddParam(cmd, "@TemplateKey", template.TemplateKey);
        AddParam(cmd, "@Name", template.Name);
        AddParam(cmd, "@Subject", template.Subject);
        AddParam(cmd, "@HtmlBody", template.HtmlBody);
        AddParam(cmd, "@IsDefault", template.IsDefault);
        AddParam(cmd, "@IsActive", template.IsActive);
        AddParam(cmd, "@UpdatedBy", template.UpdatedBy);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task MarkEmailSentAsync(int customerId, string sentBy)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_GDAP_Customer_MarkEmailSent";
        cmd.CommandTimeout = 120;
        AddParam(cmd, "@CustomerId", customerId);
        AddParam(cmd, "@SentBy", sentBy);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkEmailFailedAsync(int customerId, string sentBy, string errorMessage)
    {
        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_GDAP_Customer_MarkEmailFailed";
        cmd.CommandTimeout = 120;
        AddParam(cmd, "@CustomerId", customerId);
        AddParam(cmd, "@SentBy", sentBy);
        AddParam(cmd, "@ErrorMessage", errorMessage);
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
    {
        cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

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
            IsActive = !HasColumn(reader, "IsActive") || GetBool(reader, "IsActive"),
            PrimaryContactName = GetString(reader, "PrimaryContactName"),
            PrimaryEmail = GetString(reader, "PrimaryEmail"),
            CCEmails = GetString(reader, "CCEmails"),
            AutoSendEmail = GetBool(reader, "AutoSendEmail"),
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



    private static GdapMailTemplateModel MapMailTemplate(SqlDataReader reader)
    {
        return new GdapMailTemplateModel
        {
            Id = GetInt(reader, "Id"),
            TemplateKey = GetString(reader, "TemplateKey"),
            Name = GetString(reader, "Name"),
            Subject = GetString(reader, "Subject"),
            HtmlBody = GetString(reader, "HtmlBody"),
            IsDefault = GetBool(reader, "IsDefault"),
            IsActive = GetBool(reader, "IsActive"),
            CreatedAt = GetDate(reader, "CreatedAt"),
            CreatedBy = GetString(reader, "CreatedBy"),
            UpdatedAt = GetNullableDate(reader, "UpdatedAt"),
            UpdatedBy = GetString(reader, "UpdatedBy")
        };
    }

    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
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
