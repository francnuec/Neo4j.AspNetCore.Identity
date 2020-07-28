using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

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

        [JsonProperty] public virtual DateTimeOffset? Instant { get; private set; }
    }
}