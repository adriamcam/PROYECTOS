using System.Data;
using Microsoft.Data.SqlClient;

namespace ITQS.SupportOperationsCenter.Data;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("ReportesDB");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'ReportesDB' is not configured. Configure it in appsettings.Development.json or Azure App Service Configuration.");
        }

        return new SqlConnection(connectionString);
    }
}
