using System.Data;

namespace ITQS.SupportOperationsCenter.Data;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}
