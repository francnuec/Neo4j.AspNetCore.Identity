using System;
using Neo4jClient;
using Neo4jClient.DataAnnotations;

namespace Neo4j.AspNetCore.Identity
{
    public class Store : IStore
    {
        public Store(AnnotationsContext context)
        {
            AnnotationsContext = context ?? throw new ArgumentNullException(nameof(context));
            EntityTypes.AddAll(context.EntityService);
        }

        public virtual IGraphClient Database => AnnotationsContext.GraphClient;

        public virtual AnnotationsContext AnnotationsContext { get; protected set; }

        public EntityService EntityService => AnnotationsContext.EntityService;
    }
}