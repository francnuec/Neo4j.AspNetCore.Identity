using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neo4j.AspNetCore.Identity.Sample
{
    public class Neo4jDbSettings
    {
        public string uri { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }
}
