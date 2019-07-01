using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Neo4jClient.DataAnnotations.Cypher;
using Neo4jClient.Cypher;
using Neo4jClient.DataAnnotations;
using Neo4jClient;

namespace Neo4j.AspNetCore.Identity
{
    public class RoleStore<TRole> :
        Store,
        IRoleStore<TRole>,
        //IQueryableRoleStore<TRole>,
        IRoleClaimStore<TRole>
        where TRole : IdentityRole
    {
        /// <summary>
        ///     The _disposed
        /// </summary>
        protected bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleStore{TRole}"/> class
        /// </summary>
        /// <param name="database">The database.</param>
        public RoleStore(AnnotationsContext context) : base(context)
        {
            context.EntityService.AddEntityType(typeof(TRole));
        }

        protected internal class FindRoleResult<T>
            where T : IdentityRole
        {
            public virtual T Role { private get; set; }
            public virtual IEnumerable<IdentityClaim> Claims { private get; set; }

            public virtual T Combine()
            {
                var output = Role;

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

        public virtual void Dispose()
        {
            _disposed = true;
        }

        public virtual async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            var query = AnnotationsContext.Cypher.Create((p) => p.Pattern<TRole>("r").Prop(() => role));
            await query.ExecuteWithoutResultsAsync();

            return IdentityResult.Success;
        }

        protected ICypherFluentQuery AddClaims(ICypherFluentQuery query, IList<IdentityClaim> claims)
        {
            if ((claims == null) || (claims.Count == 0))
                return query;

            for (var i = 0; i < claims.Count; i++)
            {
                var claim = claims[i];
                query = query
                    .With("r")
                    .Merge(p => p.Pattern<TRole, IdentityClaim>("r", $"c{i}")
                    .Constrain(null, c => c.Type == claim.Type && c.Value == claim.Value))
                    .OnCreate().Set($"c{i}", () => claim);
            }

            return query;
        }

        protected virtual ICypherFluentQuery RoleMatch(TRole role)
        {
            return RoleMatch(role.Id);
        }

        protected virtual ICypherFluentQuery RoleMatch(string roleId)
        {
            return AnnotationsContext.Cypher
                .Match(p => p.Pattern<TRole>("r").Constrain(r => r.Id == roleId));
        }

        public virtual async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            var query = RoleMatch(role)
                .Set("r = {roleParam}")
                .WithParam("roleParam", role)
                ;

            //check if role has objects that were removed
            if (role.RemovedClaims.Count > 0)
            {
                //remove each one
                var existingCount = role.Claims?.Count() ?? 0;
                foreach (var claim in role.RemovedClaims)
                {
                    var idx = role.RemovedClaims.IndexOf(claim) + existingCount;
                    query = query.OptionalMatch(p => p.Pattern((TRole r) => r.Claims, $"cr{idx}", $"c{idx}")
                    .Constrain(null, c => c.Type == claim.Type && c.Value == claim.Value))
                    .Delete($"c{idx},cr{idx}");
                }
            }

            query = AddClaims(query, role.Claims?.ToList());

            await query.ExecuteWithoutResultsAsync();

            return IdentityResult.Success;
        }

        public virtual async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            ThrowIfDisposed();

            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            await RoleMatch(role)
                .OptionalMatch(p => p.Pattern((TRole r) => r.Claims, "cr", "c"))
                .Delete("r,cr,c")
                .ExecuteWithoutResultsAsync();

            return IdentityResult.Success;
        }

        /// <summary>
        ///     Throws if disposed.
        /// </summary>
        /// <exception>
        ///     <cref>System.ObjectDisposedException</cref>
        /// </exception>
        protected virtual void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public virtual Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            return Task.FromResult(role.Id);
        }

        public virtual Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            return Task.FromResult(role.Name);
        }

        public virtual Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            role.Name = roleName;

            return Task.FromResult(0);
        }

        public virtual async Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query = RoleMatch(roleId)
                .OptionalMatch(p => p.Pattern((TRole r) => r.Claims, "c"))
                .Return((r, c, l) => new FindRoleResult<TRole>
                {
                    Role = r.As<TRole>(),
                    Claims = c.CollectAs<IdentityClaim>()
                });

            var role = (await query.ResultsAsync).SingleOrDefault();

            var ret = role?.Combine();

            return ret;
        }

        public virtual async Task<TRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query = AnnotationsContext.Cypher
                .Match(p => p.Pattern<TRole>("r").Constrain(r => r.NormalizedName == normalizedName))
                .OptionalMatch(p => p.Pattern((TRole r) => r.Claims, "c"))
                .Return((r, c, l) => new FindRoleResult<TRole>
                {
                    Role = r.As<TRole>(),
                    Claims = c.CollectAs<IdentityClaim>()
                });

            var results = await query.ResultsAsync;
            var findRoleResult = results.SingleOrDefault();
            var role = findRoleResult?.Combine();

            return role;
        }

        public virtual Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            return Task.FromResult(role.NormalizedName);
        }

        public virtual Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            role.NormalizedName = normalizedName;

            return Task.FromResult(0);
        }

        public virtual Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            return Task.FromResult((IList<Claim>)role.Claims.Select(c => new Claim(c.Type, c.Value)).ToList());
        }

        public virtual Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            role.AddClaim(new IdentityClaim(claim));

            return Task.FromResult(0);
        }

        public virtual Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            foreach (var _claim in role.Claims.Where(x => x.Type == claim.Type && x.Value == claim.Value).ToList())
            {
                role.RemoveClaim(_claim);
            }

            return Task.FromResult(0);
        }
    }
}