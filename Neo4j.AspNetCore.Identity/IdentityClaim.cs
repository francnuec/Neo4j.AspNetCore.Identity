using System;
using System.Security.Claims;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neo4j.AspNetCore.Identity
{
    [Table(Labels.Claim)]
    public class IdentityClaim : IEquatable<IdentityClaim>, IEquatable<Claim>
    {
        public IdentityClaim()
        {
            CreatedOn = new Occurrence();
        }

        public IdentityClaim(Claim claim) : this()
        {
            Type = claim.Type;
            Value = claim.Value;
        }

        public IdentityClaim(string type, string value)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Type = type;
            Value = value;
        }

        [JsonProperty]
        public string Type { get; protected internal set; }

        [JsonProperty]
        public string Value { get; protected internal set; }

        [JsonProperty]
        public Occurrence CreatedOn { get; private set; }

        public Claim ToSecurityClaim()
        {
            return new Claim(Type, Value);
        }

        public bool Equals(IdentityClaim other)
        {
            return other.Type.Equals(Type)
                && other.Value.Equals(Value);
        }

        public bool Equals(Claim other)
        {
            return other.Type.Equals(Type)
                && other.Value.Equals(Value);
        }
    }
}
