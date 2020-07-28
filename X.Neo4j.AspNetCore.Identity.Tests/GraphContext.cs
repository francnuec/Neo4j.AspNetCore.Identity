using Neo4jClient;
using Neo4jClient.DataAnnotations;
using Neo4jClient.DataAnnotations.Serialization;

namespace X.Neo4j.AspNetCore.Identity.Tests
{
    public class GraphContext : AnnotationsContext
    {
        public GraphContext(IGraphClient graphClient)
            : base(graphClient)
        {
        }

        public GraphContext(IGraphClient graphClient, EntityResolver resolver, EntityService entityService)
            : base(graphClient, resolver, entityService)
        {
        }

        public GraphContext(IGraphClient graphClient, EntityConverter converter, EntityService entityService)
            : base(graphClient, converter, entityService)
        {
        }
    }
}