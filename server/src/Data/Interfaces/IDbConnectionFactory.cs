using System.Data.Common;

namespace server.src.Data.Interfaces;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
