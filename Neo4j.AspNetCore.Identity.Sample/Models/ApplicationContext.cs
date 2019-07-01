using Neo4jClient;
using Neo4jClient.DataAnnotations;
using Neo4jClient.DataAnnotations.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neo4j.AspNetCore.Identity.Sample.Models
{
    public class ApplicationContext : AnnotationsContext
    {
        public ApplicationContext(IGraphClient graphClient) : base(graphClient)
        {
        }

        public ApplicationContext(IGraphClient graphClient, EntityService entityService) : base(graphClient, entityService)
        {
        }

        public ApplicationContext(IGraphClient graphClient, EntityResolver resolver) : base(graphClient, resolver)
        {
        }

        public ApplicationContext(IGraphClient graphClient, EntityConverter converter) : base(graphClient, converter)
        {
        }

        public ApplicationContext(IGraphClient graphClient, EntityResolver resolver, EntityService entityService) : base(graphClient, resolver, entityService)
        {
        }

        public ApplicationContext(IGraphClient graphClient, EntityConverter converter, EntityService entityService) : base(graphClient, converter, entityService)
        {
        }

        public EntitySet<ApplicationUser> ApplicationUsers { get; set; }
        public EntitySet<IdentityRole> ApplicationRoles { get; set; }
    }
}
