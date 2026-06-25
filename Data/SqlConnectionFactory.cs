using System.Data;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ITQS.SupportOperationsCenter.Data;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly SqlSettings _sql;
    private readonly KeyVaultSettings _kv;
    private readonly ILogger<SqlConnectionFactory> _logger;

    private string? _cachedUser;
    private string? _cachedPassword;

    public SqlConnectionFactory(
        IOptions<SqlSettings> sqlOptions,
        IOptions<KeyVaultSettings> keyVaultOptions,
        ILogger<SqlConnectionFactory> logger)
    {
        _sql = sqlOptions.Value;
        _kv = keyVaultOptions.Value;
        _logger = logger;
    }

    public IDbConnection CreateConnection()
    {
        var user = GetSecret(_kv.SqlUserSecret, ref _cachedUser);
        var password = GetSecret(_kv.SqlPasswordSecret, ref _cachedPassword);

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = _sql.Server,
            InitialCatalog = _sql.Database,
            UserID = user,
            Password = password,
            Encrypt = true,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };

        return new SqlConnection(builder.ConnectionString);
    }

    private string GetSecret(string secretName, ref string? cachedValue)
    {
        if (!string.IsNullOrWhiteSpace(cachedValue))
            return cachedValue;

        if (string.IsNullOrWhiteSpace(secretName))
            throw new InvalidOperationException("El nombre del secreto de Key Vault está vacío.");

        try
        {
            var client = new SecretClient(
                new Uri($"https://{_kv.VaultName}.vault.azure.net/"),
                new DefaultAzureCredential());

            cachedValue = client.GetSecret(secretName).Value.Value;

            return cachedValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo secreto {SecretName} desde Key Vault.", secretName);
            throw;
        }
    }
}