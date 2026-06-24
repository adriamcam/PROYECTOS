using Microsoft.Data.SqlClient;

namespace ITQS.SupportOperationsCenter.Services;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}

public sealed class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    public SqlConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString("ReportesDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:ReportesDb no está configurado.");

        return new SqlConnection(connectionString);
    }
}
