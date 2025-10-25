using System.Data;

namespace Cerberus.Infrastructure;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
