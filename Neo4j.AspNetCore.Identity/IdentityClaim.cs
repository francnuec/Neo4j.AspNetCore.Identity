using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using Newtonsoft.Json;

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
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value; //?? throw new ArgumentNullException(nameof(value));
        }

        [JsonProperty] public virtual string Type { get; protected internal set; }

        [JsonProperty] public virtual string Value { get; protected internal set; }

        [JsonProperty] public virtual Occurrence CreatedOn { get; private set; }

        public bool Equals(Claim other)
        {
            return Type.Equals(other?.Type)
                   && (Value?.Equals(other?.Value)
                       ?? Value == null && other?.Value == null);
        }

        public bool Equals(IdentityClaim other)
        {
            return Type.Equals(other?.Type)
                   && (Value?.Equals(other?.Value)
                       ?? Value == null && other?.Value == null);
        }

        public Claim ToSecurityClaim()
        {
            return new Claim(Type, Value);
        }
    }
}