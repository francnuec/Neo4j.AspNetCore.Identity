using System;
using System.Collections.Generic;
using System.Security.Claims;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neo4j.AspNetCore.Identity
{
    [Table(Labels.Role)]
    /// <summary>
    /// Represents a Role entity
    /// </summary>
    public class IdentityRole : IEquatable<IdentityRole>, IEquatable<string>
    {
        public IdentityRole()
        {
            Id = "role_" + Guid.NewGuid().ToString("N");
            CreatedOn = new Occurrence();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="roleName"></param>
        public IdentityRole(string roleName) : this()
        {
            Name = roleName;
        }

        [JsonProperty]
        /// <summary>
        /// Role Id
        /// </summary>
        public virtual string Id { get; protected internal set; }

        [JsonProperty]
        /// <summary>
        /// Role name
        /// </summary>
        public virtual string Name { get; protected internal set; }

        [JsonProperty]
        public virtual string NormalizedName { get; protected internal set; }

        [JsonProperty]
        /// <summary>
        /// A random value that should change whenever a role is persisted to the store
        /// </summary>
        public virtual string ConcurrencyStamp { get; protected internal set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Collection of claims in the role
        /// </summary>
        [Column(Relationship.Claims)]
        public IEnumerable<IdentityClaim> Claims { get; protected internal set; } = new List<IdentityClaim>();

        [JsonIgnore]
        protected internal List<IdentityClaim> RemovedClaims { get; } = new List<IdentityClaim>();

        [JsonProperty]
        public virtual Occurrence CreatedOn { get; private set; }

        [InverseProperty("Role")]
        public virtual IEnumerable<IdentityUser_Role> Users { get; }

        public virtual void AddClaim(Claim claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            AddClaim(new IdentityClaim(claim));
        }

        public virtual void AddClaim(IdentityClaim claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            ((IList<IdentityClaim>)Claims).Add(claim);
        }

        public virtual void RemoveClaim(IdentityClaim claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            ((IList<IdentityClaim>)Claims).Remove(claim);
            RemovedClaims.Add(claim);
        }

        public bool Equals(IdentityRole other)
        {
            return other.NormalizedName.Equals(NormalizedName);
        }

        public bool Equals(string name)
        {
            return name.Equals(NormalizedName)
                || name.Equals(Name);
        }
    }
}