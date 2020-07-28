using Neo4jClient;
using Neo4jClient.DataAnnotations;

namespace Neo4j.AspNetCore.Identity
{
    public interface IStore : IHaveAnnotationsContext
    {
        IGraphClient Database { get; }
    }
}