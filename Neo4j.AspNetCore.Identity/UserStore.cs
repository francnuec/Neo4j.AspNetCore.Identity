using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Neo4jClient.Cypher;
using Neo4jClient.DataAnnotations.Cypher;
using Neo4jClient;
using Neo4jClient.DataAnnotations;

namespace Neo4j.AspNetCore.Identity
{
    public class UserStore<TUser>
        : Store,
        IUserStore<TUser>,
        IUserLoginStore<TUser>,
        IUserRoleStore<TUser>,
        IUserClaimStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IUserEmailStore<TUser>,
        IUserLockoutStore<TUser>,
        IUserPhoneNumberStore<TUser>,
        //IQueryableUserStore<TUser>,
        IUserTwoFactorStore<TUser>
        where TUser : IdentityUser
    {
        static UserStore()
        {
            EntityTypes.AddAll();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserStore{TUser}"/> class using an already initialized Neo4j GrpahClient.
        /// </summary>
        public UserStore(IGraphClient database)
        {
            Database = database;
            Neo4jAnnotations.AddEntityType(typeof(TUser));
        }

        internal class FindUserResult<T>
            where T : IdentityUser//, new()
        {
            public T User { private get; set; }
            public IEnumerable<IdentityExternalLogin> Logins { private get; set; }
            public IEnumerable<IdentityClaim> Claims { private get; set; }

            public T Combine()
            {
                var output = User;
                if (Logins != null)
                {
                    foreach (var login in Logins)
                    {
                        output.AddLogin(login); //.Select(l => l.ToUserLoginInfo()));
                    }
                }

                if (Claims != null)
                {
                    foreach (var claim in Claims)
                    {
                        output.AddClaim(claim);
                    }
                }

                return output;
            }
        }

        private static ICypherFluentQuery AddClaims(ICypherFluentQuery query, IList<IdentityClaim> claims)
        {
            if ((claims == null) || (claims.Count == 0))
                return query;

            for (var i = 0; i < claims.Count; i++)
            {
                var claim = claims[i];
                query = query
                    .With("u")
                    .Merge(p => p.Pattern<TUser, IdentityClaim>("u", $"c{i}")
                    .Constrain(null, c => c.Type == claim.Type && c.Value == claim.Value))
                    .OnCreate().Set($"c{i}", () => claim);
            }

            return query;
        }

        private static ICypherFluentQuery AddLogins(ICypherFluentQuery query, IList<IdentityExternalLogin> logins)
        {
            if ((logins == null) || (logins.Count == 0))
                return query;

            for (var i = 0; i < logins.Count; i++)
            {
                var login = logins[i];
                query = query
                    .With("u")
                    .Merge(p => p.Pattern<TUser, IdentityExternalLogin>("u", $"l{i}")
                    .Constrain(null, l => l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey))
                    .OnCreate().Set($"l{i}", () => login);
            }
            return query;
        }

        private static ICypherFluentQuery AddRoles(ICypherFluentQuery query, IList<string> roles, string userId)
        {
            if ((roles == null) || (roles.Count == 0))
                return query;

            for (var i = 0; i < roles.Count; i++)
            {
                var roleVar = $"r{i}";
                var userRoleVar = $"ur{i}";
                query = query
                    .With("u")
                    .Match(p => p.Pattern<IdentityRole>(roleVar).Constrain(r => r.NormalizedName == roles[i]))
                    .Merge(p => p.Pattern<TUser, IdentityUser_Role, IdentityRole>("u", userRoleVar, roleVar)
                    .Constrain(null, ur => ur.RoleId == Vars.Get<IdentityRole>(roleVar).Id && ur.UserId == userId, null))
                    .OnCreate().Set<IdentityUser_Role>(ur => ur.CreatedOn == new Occurrence(), userRoleVar);
            }

            return query;
        }

        private ICypherFluentQuery UserMatch(TUser user)
        {
            return UserMatch(user.Id);
        }

        private ICypherFluentQuery UserMatch(string userId)
        {
            return Database.Cypher
                .Match(p => p.Pattern<TUser>("u").Constrain(u => u.Id == userId));
        }

        #region IUserStore
        public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var query = Database.Cypher
                .Create(p => p.Pattern<TUser>("u").Prop(() => user));

            await query.ExecuteWithoutResultsAsync();

            //await Database.InsertAsync<TUser>(user);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            await UserMatch(user)
                .OptionalMatch(p => p.Pattern((TUser u) => u.Logins, "lr", "l"))
                .OptionalMatch(p => p.Pattern((TUser u) => u.Claims, "cr", "c"))
                .OptionalMatch(p => p.Pattern((TUser u) => u.RolesRelationship, "rr", "r"))
                .Delete("u,lr,cr,rr,l,c")
                .ExecuteWithoutResultsAsync();

            return IdentityResult.Success;
        }

        public async Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var query = UserMatch(userId)
                .OptionalMatch(p => p.Pattern((TUser u) => u.Logins, "l"))
                .OptionalMatch(p => p.Pattern((TUser u) => u.Claims, "c"))
                .Return((u, c, l) => new FindUserResult<TUser>
                {
                    User = u.As<TUser>(),
                    Logins = l.CollectAs<IdentityExternalLogin>(),
                    Claims = c.CollectAs<IdentityClaim>()
                });

            var user = (await query.ResultsAsync).SingleOrDefault();

            var ret = user?.Combine();

            return ret;
        }

        public async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var query = Database.Cypher
                .Match(p => p.Pattern<TUser>("u").Constrain(u => u.NormalizedUserName == normalizedUserName))
                .OptionalMatch(p => p.Pattern((TUser u) => u.Logins, "l"))
                .OptionalMatch(p => p.Pattern((TUser u) => u.Claims, "c"))
                .Return((u, c, l) => new FindUserResult<TUser>
                {
                    User = u.As<TUser>(),
                    Logins = l.CollectAs<IdentityExternalLogin>(),
                    Claims = c.CollectAs<IdentityClaim>()
                });

            var results = await query.ResultsAsync;
            var findUserResult = results.SingleOrDefault();
            var user = findUserResult?.Combine();

            return user;
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.NormalizedUserName);
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.UserName);
        }

        public async Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.SetNormalizedUserName(normalizedName);
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.SetUserName(userName);

            return Task.FromResult(0);
        }

        public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var query = UserMatch(user)
                .Set("u", () => user);

            //check if user has objects that were removed
            if (user.RemovedObjects.Count > 0)
            {
                //remove each one from db
                var logins = user.RemovedObjects.OfType<IdentityExternalLogin>().ToList();
                var existingCount = user.Logins?.Count() ?? 0;
                foreach (var login in logins)
                {
                    var idx = logins.IndexOf(login) + existingCount;
                    query = query.OptionalMatch(p => p.Pattern((TUser u) => u.Logins, $"lr{idx}", $"l{idx}")
                    .Constrain(null, l => l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey))
                    .Delete($"l{idx},lr{idx}");
                }

                var claims = user.RemovedObjects.OfType<IdentityClaim>().ToList();
                existingCount = user.Claims?.Count() ?? 0;
                foreach (var claim in claims)
                {
                    var idx = claims.IndexOf(claim) + existingCount;
                    query = query.OptionalMatch(p => p.Pattern((TUser u) => u.Claims, $"cr{idx}", $"c{idx}")
                    .Constrain(null, c => c.Type == claim.Type && c.Value == claim.Value))
                    .Delete($"c{idx},cr{idx}");
                }

                var roles = user.RemovedObjects.OfType<string>().ToList();
                existingCount = user.Roles?.Count() ?? 0;
                foreach (var role in roles)
                {
                    var idx = roles.IndexOf(role) + existingCount;
                    query = query.OptionalMatch(p => p.Pattern<TUser, IdentityUser_Role, IdentityRole>((TUser u) => u.RolesRelationship, $"rr{idx}", $"r{idx}")
                    .Constrain(null, rr => rr.UserId == user.Id, r => r.NormalizedName == role))
                    .Delete($"rr{idx}"); //never delete roles here
                }
            }

            query = AddClaims(query, user.Claims?.ToList());
            query = AddLogins(query, user.Logins?.ToList());
            query = AddRoles(query, user.Roles?.ToList(), user.Id);

            await query.ExecuteWithoutResultsAsync();

            return IdentityResult.Success;
        }
        #endregion

        #region IDisposable Support
        private bool _disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~UserStore2() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Throws if disposed.
        /// </summary>
        /// <exception>
        ///     <cref>System.ObjectDisposedException</cref>
        /// </exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }
        #endregion

        #region IUserLoginStore
        public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            foreach (var _login in user.Logins.Where(x => x.LoginProvider == login.LoginProvider
            && x.ProviderKey == login.ProviderKey).ToList())
            {
                user.RemoveLogin(_login);
            }

            user.AddLogin(new IdentityExternalLogin(login));

            return Task.FromResult(0);
        }

        public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            foreach (var _login in user.Logins.Where(x => x.LoginProvider == loginProvider
            && x.ProviderKey == providerKey).ToList())
            {
                user.RemoveLogin(_login);
            }

            return Task.FromResult(0);
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            IList<UserLoginInfo> logins = user.Logins.Select(info => info.ToUserLoginInfo()).ToList();

            return Task.FromResult(logins);
        }

        public async Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var results = await Database.Cypher
                .Match(p => p.Pattern<IdentityExternalLogin, TUser>("login", "u")
                .Constrain(login => login.ProviderKey == providerKey
                    && login.LoginProvider == loginProvider))
                //.Match($"(l:{Labels.Login})<-[:{Relationship.HasLogin}]-(u:{UserLabel})")
                //.Where((UserLoginInfo l) => l.ProviderKey == login.ProviderKey)
                //.AndWhere((UserLoginInfo l) => l.LoginProvider == login.LoginProvider)
                //.OptionalMatch($"(u)-[:{Relationship.HasClaim}]->(c:{Labels.Claim})")
                .OptionalMatch(p => p.Pattern((TUser u) => u.Logins, "l"))
                .OptionalMatch(p => p.Pattern((TUser u) => u.Claims, "c"))
                .Return((u, c, l) => new FindUserResult<TUser>
                {
                    User = u.As<TUser>(),
                    Logins = l.CollectAs<IdentityExternalLogin>(),
                    Claims = c.CollectAs<IdentityClaim>()
                }).ResultsAsync;

            var result = results.SingleOrDefault();
            return result?.Combine();
        }
        #endregion

        #region IUserRoleStore
        public async Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!user.Roles.Contains(roleName, StringComparer.CurrentCultureIgnoreCase))
            {
                user.AddRole(roleName);
            }
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var roles = user.Roles.Where(x => string.Equals(x, roleName, StringComparison.CurrentCultureIgnoreCase)).ToArray().Distinct();

            foreach (var role in roles)
                user.RemoveRole(role);

            return Task.FromResult(0);
        }

        public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult<IList<string>>(user.Roles.ToList());
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Roles.Contains(roleName, StringComparer.CurrentCultureIgnoreCase));
        }

        public async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(roleName))
                throw new ArgumentNullException(nameof(roleName));

            var query = Database.Cypher
                .Match(p => p.Pattern((TUser u) => u.RolesRelationship, rr => rr.Role, "r")
                .Constrain(null, null, r => r.NormalizedName == roleName))
                .OptionalMatch(p => p.Pattern((TUser u) => u.Logins, "l"))
                .OptionalMatch(p => p.Pattern((TUser u) => u.Claims, "c"))
                .Return((u, c, l) => new FindUserResult<TUser>
                {
                    User = u.As<TUser>(),
                    Logins = l.CollectAs<IdentityExternalLogin>(),
                    Claims = c.CollectAs<IdentityClaim>()
                });

            var results = await query.ResultsAsync;
            var usersInRole = results.Select(u => u.Combine()).ToList();

            return usersInRole;
        }
        #endregion

        #region IUserClaimStore
        public Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            IList<Claim> result = user.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();

            return Task.FromResult(result);
        }

        public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            foreach (var claim in claims)
            {
                user.AddClaim(new IdentityClaim(claim));
            }

            return Task.FromResult(0);
        }

        public Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));


            foreach (var _claim in user.Claims.Where(x => x.Type == claim.Type && x.Value == claim.Value).ToList())
            {
                user.RemoveClaim(_claim);
            }

            user.AddClaim(new IdentityClaim(claim));

            return Task.FromResult(0);
        }

        public Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            foreach (var _claim in user.Claims.Where(x => claims.Any(claim => x.Type == claim.Type && x.Value == claim.Value)).ToList())
            {
                user.RemoveClaim(_claim);
            }

            return Task.FromResult(0);
        }

        public async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (claim == null)
                throw new ArgumentNullException(nameof(claim));

            var results = await Database.Cypher
                .Match(p => p.Pattern<IdentityClaim, TUser>("c", "u"))
                .Where((IdentityClaim c) => c.Type == claim.Type)
                .AndWhere((IdentityClaim c) => c.Value == claim.Value)
                .OptionalMatch(p => p.Pattern<TUser, IdentityExternalLogin>("u", "l"))
                .Return((u, c, l) => new FindUserResult<TUser>
                {
                    User = u.As<TUser>(),
                    Logins = l.CollectAs<IdentityExternalLogin>(),
                    Claims = c.CollectAs<IdentityClaim>()
                }).ResultsAsync;

            var result = results?.Select(u => u.Combine()).ToList();
            return result;
        }
        #endregion

        #region IUserPasswordStore
        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.SetPasswordHash(passwordHash);

            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.PasswordHash != null);
        }
        #endregion

        #region IUserSecurityStampStore
        public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.SetSecurityStamp(stamp);

            return Task.FromResult(0);
        }

        public Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.SecurityStamp);
        }
        #endregion

        #region IUserEmailStore
        public Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.SetEmail(email);

            return Task.FromResult(0);
        }

        public Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Email?.Value);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Email?.IsConfirmed() == true);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (confirmed)
                user.Email?.SetConfirmed();
            else
                user.Email?.SetUnconfirmed();

            return Task.FromResult(0);
        }

        public async Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var query = Database.Cypher
                .Match(p => p.Pattern<TUser>("u").Constrain(u => u.Email.NormalizedValue == normalizedEmail))
                .OptionalMatch(p => p.Pattern((TUser u) => u.Logins, "l"))
                .OptionalMatch(p => p.Pattern((TUser u) => u.Claims, "c"))
                .Return((u, c, l) => new FindUserResult<TUser>
                {
                    User = u.As<TUser>(),
                    Logins = l.CollectAs<IdentityExternalLogin>(),
                    Claims = c.CollectAs<IdentityClaim>()
                });

            var results = await query.ResultsAsync;
            var findUserResult = results.SingleOrDefault();
            var user = findUserResult?.Combine();

            return user;
        }

        public Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.Email?.NormalizedValue);
        }

        public Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.Email?.SetNormalizedEmail(normalizedEmail);

            return Task.FromResult(0);
        }
        #endregion

        #region IUserLockoutStore
        public Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.LockoutEndDate?.Instant);
        }

        public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.LockUntil(lockoutEnd);

            return Task.FromResult(0);
        }

        public Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.SetAccessFailedCount(user.AccessFailedCount + 1);

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.ResetAccessFailedCount();

            return Task.FromResult(0);
        }

        public Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.IsLockoutEnabled);
        }

        public Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (enabled)
                user.EnableLockout();
            else
                user.DisableLockout();

            return Task.FromResult(0);
        }

        #endregion

        #region IUserPhoneNumberStore
        public Task SetPhoneNumberAsync(TUser user, string phoneNumber, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.SetPhoneNumber(phoneNumber);

            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.PhoneNumber?.Value);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.PhoneNumber?.IsConfirmed() == true);
        }

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (confirmed)
                user.PhoneNumber?.SetConfirmed();
            else
                user.PhoneNumber?.SetUnconfirmed();

            return Task.FromResult(0);
        }

        #endregion

        #region IUserTwoFactorStore
        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (enabled)
                user.EnableTwoFactorAuthentication();
            else
                user.DisableTwoFactorAuthentication();

            return Task.FromResult(0);
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return Task.FromResult(user.IsTwoFactorEnabled);
        }

        #endregion
    }
}
