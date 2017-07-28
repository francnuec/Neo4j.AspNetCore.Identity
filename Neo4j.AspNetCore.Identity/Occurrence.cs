using System;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neo4j.AspNetCore.Identity
{
    [ComplexType]
    public class Occurrence
    {
        public Occurrence() : this(DateTimeOffset.UtcNow)
        {
        }

        public Occurrence(DateTimeOffset? occuranceInstantUtc)
        {
            Instant = occuranceInstantUtc;
        }

        [JsonProperty]
        public DateTimeOffset? Instant { get; private set; }
    }
}
