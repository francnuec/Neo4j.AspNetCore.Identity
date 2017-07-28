using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Neo4j.AspNetCore.Identity
{
    /// <summary>
    /// (user)-[user_role]->(role)
    /// </summary>
    [Table(Relationship.Roles)]
    public class IdentityUser_Role
    {
        public IdentityUser_Role()
        {
            CreatedOn = new Occurrence();
        }

        [Key]
        [Column(Order = 1)]
        public string RoleId { get; set; }

        public IdentityRole Role { get; set; }

        [Key]
        [Column(Order = 2)]
        public string UserId { get; set; }

        public IdentityUser User { get; set; }

        [JsonProperty]
        public Occurrence CreatedOn { get; private set; }
    }
}
