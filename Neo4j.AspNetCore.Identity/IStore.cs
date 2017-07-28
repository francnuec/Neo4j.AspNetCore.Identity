
using Neo4jClient;

namespace Neo4j.AspNetCore.Identity
{
    public interface IStore
    {
        IGraphClient Database { get; set; }
        //IArangoDatabase Database { get; set; }
        //IArangoDatabase GetDatabaseFromSqlStyle(string connectionString);
    }
}
