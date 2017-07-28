using System;
using Neo4jClient;

namespace Neo4j.AspNetCore.Identity
{
    public class Store : IStore
    {
        public IGraphClient Database { get; set; }
    }
}
