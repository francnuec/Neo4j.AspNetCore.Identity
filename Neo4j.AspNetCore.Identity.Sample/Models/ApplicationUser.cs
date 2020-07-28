namespace Neo4j.AspNetCore.Identity.Sample.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
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