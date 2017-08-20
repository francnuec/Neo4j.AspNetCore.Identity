using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Neo4j.AspNetCore.Identity
{
    [Table(Labels.User)]
    public class IdentityUser
    {
        private readonly List<string> _roles;
        private readonly List<IdentityClaim> _claims;
        private readonly List<IdentityExternalLogin> _logins;

        public IdentityUser()
        {
            Id = GenerateId();

            CreatedOn = new Occurrence();
            //DeletedOn = new Occurrence(null);
            LockoutEndDate = new FutureOccurrence(null);
            Email = new UserEmail();
            PhoneNumber = new UserPhoneNumber();

            _claims = new List<IdentityClaim>();
            _logins = new List<IdentityExternalLogin>();
            _roles = new List<string>();
        }

        public IdentityUser(string userName) : this()
        {
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
        }

        public IdentityUser(string userName, string email) : this(userName)
        {
            if (email != null)
            {
                Email = new UserEmail(email);
            }
        }

        public IdentityUser(string userName, UserEmail email) : this(userName)
        {
            if (email != null)
            {
                Email = email;
            }
        }

        [JsonProperty]
        public virtual string Id { get; protected set; }

        [JsonProperty]
        public virtual string UserName { get; protected set; }

        [JsonProperty]
        public virtual string NormalizedUserName { get; protected set; }

        [JsonProperty]
        public virtual UserEmail Email { get; protected set; }

        [JsonProperty]
        public virtual UserPhoneNumber PhoneNumber { get; protected set; }

        [JsonProperty]
        public virtual string PasswordHash { get; protected set; }

        [JsonProperty]
        public virtual string SecurityStamp { get; protected set; }

        [JsonProperty]
        public virtual bool IsTwoFactorEnabled { get; protected set; }

        [Column(Relationship.Claims)]
        public virtual IEnumerable<IdentityClaim> Claims
        {
            get
            {
                return _claims;
            }

            protected set
            {
                if (value != null)
                {
                    _claims.AddRange(value.Where(c => c != null));
                }
            }
        }

        [Column(Relationship.Logins)]
        public virtual IEnumerable<IdentityExternalLogin> Logins
        {
            get
            {
                return _logins;
            }

            protected set
            {
                if (value != null)
                {
                    _logins.AddRange(value.Where(l => l != null));
                }
            }
        }

        [JsonProperty]
        public virtual IEnumerable<string> Roles
        {
            get
            {
                return _roles;
            }
            protected set
            {
                if (value != null)
                {
                    _roles.AddRange(value.Where(r => !string.IsNullOrWhiteSpace(r)));
                }
            }
        }

        [JsonIgnore]
        protected internal List<object> RemovedObjects { get; } = new List<object>();

        [InverseProperty("User")]
        public virtual IEnumerable<IdentityUser_Role> RolesRelationship { get; protected set; }

        [JsonProperty]
        public virtual int AccessFailedCount { get; protected set; }

        [JsonProperty]
        public virtual bool IsLockoutEnabled { get; protected set; }

        [JsonProperty]
        public virtual FutureOccurrence LockoutEndDate { get; protected set; }

        [JsonProperty]
        public virtual Occurrence CreatedOn { get; protected set; }

        //[JsonProperty]
        //public virtual Occurrence DeletedOn { get; protected set; }


        public virtual void EnableTwoFactorAuthentication()
        {
            IsTwoFactorEnabled = true;
        }

        public virtual void DisableTwoFactorAuthentication()
        {
            IsTwoFactorEnabled = false;
        }

        public virtual void EnableLockout()
        {
            IsLockoutEnabled = true;
        }

        public virtual void DisableLockout()
        {
            IsLockoutEnabled = false;
        }

        public virtual void SetUserName (string userName)
        {
            UserName = userName;
        }

        public virtual void SetEmail(string email)
        {
            var userEmail = new UserEmail(email);
            SetEmail(userEmail);
        }

        public virtual void SetEmail(UserEmail userEmail)
        {
            Email = userEmail;
        }

        public virtual void SetNormalizedUserName(string normalizedUserName)
        {
            if (normalizedUserName == null)
            {
                throw new ArgumentNullException(nameof(normalizedUserName));
            }

            NormalizedUserName = normalizedUserName;
        }

        public virtual void SetPhoneNumber(string phoneNumber)
        {
            var userPhoneNumber = new UserPhoneNumber(phoneNumber);
            SetPhoneNumber(userPhoneNumber);
        }

        public virtual void SetPhoneNumber(UserPhoneNumber userPhoneNumber)
        {
            PhoneNumber = userPhoneNumber;
        }

        public virtual void SetPasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
        }

        public virtual void SetSecurityStamp(string securityStamp)
        {
            SecurityStamp = securityStamp;
        }

        public virtual void SetAccessFailedCount(int accessFailedCount)
        {
            AccessFailedCount = accessFailedCount;
        }

        public virtual void ResetAccessFailedCount()
        {
            AccessFailedCount = 0;
        }

        public virtual void LockUntil(DateTimeOffset? lockoutEndDate)
        {
            LockoutEndDate = new FutureOccurrence(lockoutEndDate);
        }

        public virtual void AddClaim(Claim claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            AddClaim(new IdentityClaim(claim));
        }

        public virtual void AddClaim(IdentityClaim userClaim)
        {
            if (userClaim == null)
            {
                throw new ArgumentNullException(nameof(userClaim));
            }

            _claims.Add(userClaim);
        }

        public virtual void RemoveClaim(IdentityClaim userClaim)
        {
            if (userClaim == null)
            {
                throw new ArgumentNullException(nameof(userClaim));
            }

            _claims.Remove(userClaim);
            RemovedObjects.Add(userClaim);
        }

        public virtual void AddLogin(IdentityExternalLogin userLogin)
        {
            if (userLogin == null)
            {
                throw new ArgumentNullException(nameof(userLogin));
            }

            _logins.Add(userLogin);
        }

        public virtual void RemoveLogin(IdentityExternalLogin userLogin)
        {
            if (userLogin == null)
            {
                throw new ArgumentNullException(nameof(userLogin));
            }

            _logins.Remove(userLogin);
            RemovedObjects.Add(userLogin);
        }

        public virtual void AddRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentNullException(nameof(role));
            }

            _roles.Add(role);
        }

        public virtual void RemoveRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentNullException(nameof(role));
            }

            _roles.Remove(role);
            RemovedObjects.Add(role);
        }

        //public void Delete()
        //{
        //    if (DeletedOn != null && DeletedOn.Instant == null)
        //    {
        //        throw new InvalidOperationException($"User '{Id}' has already been deleted.");
        //    }

        //    DeletedOn = new Occurrence();
        //}

        private static string GenerateId()
        {
            return $"user_{Guid.NewGuid().ToString("N")}";
        }
    }
}