using Neo4j.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neo4j.AspNetCore.Identity.Sample.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser() : base()
        {
        }

        public ApplicationUser(string userName, string email) : base(userName, email)
        {
        }

        public ApplicationUser(string userName, UserEmail email) : base(userName, email)
        {
        }

        public ApplicationUser(string userName) : base(userName)
        {
        }
    }
}
