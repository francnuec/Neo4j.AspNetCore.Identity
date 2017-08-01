using System;
using Neo4jClient;

namespace Neo4j.AspNetCore.Identity
{
    public class Store : IStore
    {
        public virtual IGraphClient Database { get; set; }
    }
}
