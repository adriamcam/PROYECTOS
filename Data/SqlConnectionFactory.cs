using System.Data;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
namespace ITQS.SupportOperationsCenter.Data;
public sealed class SqlConnectionFactory:ISqlConnectionFactory{
private readonly SqlSettings _sql; private readonly KeyVaultSettings _kv;
private string? _pwd;
public SqlConnectionFactory(IOptions<SqlSettings> s,IOptions<KeyVaultSettings> k,ILogger<SqlConnectionFactory> l){_sql=s.Value;_kv=k.Value;}
public IDbConnection CreateConnection(){var cs=new SqlConnectionStringBuilder{DataSource=_sql.Server,InitialCatalog=_sql.Database,UserID=_sql.UserId,Password=GetPwd(),Encrypt=true,TrustServerCertificate=true};return new SqlConnection(cs.ConnectionString);}
private string GetPwd(){if(!string.IsNullOrEmpty(_pwd))return _pwd;var c=new SecretClient(new Uri($"https://{_kv.VaultName}.vault.azure.net/"),new DefaultAzureCredential());_pwd=c.GetSecret(_kv.SqlPasswordSecret).Value.Value;return _pwd;}}