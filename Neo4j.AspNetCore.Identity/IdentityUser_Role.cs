using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Neo4j.AspNetCore.Identity
{
    /// <summary>
    ///     (user)-[user_role]->(role)
    /// </summary>
    [Table(Relationship.Roles)]
    public class IdentityUser_Role
    {
        public IdentityUser_Role()
        {
            CreatedOn = new Occurrence();
        }

        [Key] [Column(Order = 1)] public virtual string RoleId { get; set; }

        public virtual IdentityRole Role { get; set; }

        [Key] [Column(Order = 2)] public virtual string UserId { get; set; }

        public virtual IdentityUser User { get; set; }

        [JsonProperty] public virtual Occurrence CreatedOn { get; private set; }
    }
}