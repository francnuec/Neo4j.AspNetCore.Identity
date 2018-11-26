namespace Neo4j.AspNetCore.Identity
{
    using Neo4j.AspNetCore.Identity;
    using Neo4jClient.DataAnnotations;
    using System;

    /// <summary>Consts for the Relationships used throughout Neo4j.</summary>
    public static class Relationship
    {
        /// <summary>Relationship representing whether a user has another Login - HAS_LOGIN</summary>
        public const string Logins = "HAS_LOGIN";
        /// <summary>Relationship representing whether a user has a claim - HAS_LOGIN</summary>
        public const string Claims = "HAS_CLAIM";

        public const string Roles = "IN_ROLE";
    }

    /// <summary>Consts for the Labels used throughout Neo4j.</summary>
    public static class Labels
    {
        /// <summary>Login label, used by <see cref="UserLoginInfo"/> class.</summary>
        public const string Login = "IdentityLogin";


        /// <summary>User label, used by <see cref="ApplicationUser"/> class.</summary>
        //[Obsolete("Should not be used, as ApplicationUser.Labels will be used (if not set by user)")]
        public const string User = "IdentityUser";

        /// <summary>Claim label, used by <see cref="IdentityUserClaim"/> class.</summary>
        public const string Claim = "IdentityClaim";

        public const string Role = "IdentityRole";
    }

    public static class EntityTypes
    {
        public static Type[] All = new Type[]
        {
            typeof(IdentityUser),
            typeof(IdentityClaim),
            typeof(IdentityExternalLogin),
            typeof(IdentityRole),
            typeof(IdentityUser_Role),
            typeof(Occurrence),
            typeof(FutureOccurrence),
            typeof(ConfirmationOccurrence),
            typeof(UserContactRecord),
            typeof(UserEmail),
            typeof(UserPhoneNumber),
        };

        public static void AddAll(EntityService entityService)
        {
            if (entityService == null)
            {
                throw new ArgumentNullException(nameof(entityService));
            }

            foreach (var type in All)
                entityService.AddEntityType(type);
        }
    }
}