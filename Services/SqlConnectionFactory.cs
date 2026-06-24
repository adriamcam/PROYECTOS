using Microsoft.Data.SqlClient;
using System.Data;

namespace ITQS.SupportOperationsCenter.Services;

public sealed class SqlConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("ReportesDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:ReportesDb no está configurado.");
        }

        return new SqlConnection(connectionString);
    }
}
