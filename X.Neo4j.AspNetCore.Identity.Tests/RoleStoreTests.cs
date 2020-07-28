using System;
using System.Linq;
using System.Security.Claims;
using Neo4j.AspNetCore.Identity;
using Neo4jClient;
using Neo4jClient.DataAnnotations;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace X.Neo4j.AspNetCore.Identity.Tests
{
    [Collection("Sequential")]
    public class RoleStoreTests
    {
        public static IGraphClient CreateGraphClient()
        {
            var client = new BoltGraphClient(new Uri("BOLT_URL"), "NEO4J_USER", "NEO4J_PASSWORD")
            {
                JsonContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            client.ConnectAsync().Wait();
            return client.WithAnnotations<GraphContext>();
        }

        public static RoleStore<IdentityRole> CreateStore(IGraphClient gclient)
        {
            return new RoleStore<IdentityRole>(new GraphContext(gclient));
        }

        private IdentityRole FetchTestRole(IGraphClient client, RoleStore<IdentityRole> rs)
        {
            var f = client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'")
                .AsAnnotatedQuery()
                .Return(n => n.As<IdentityRole>())
                .AsCypherQuery().ResultsAsync.Result.FirstOrDefault();
            return rs.FindByIdAsync(f.Id).Result;
        }

        private IdentityRole CreateRole(IGraphClient client, RoleStore<IdentityRole> rs)
        {
            var result = rs.CreateAsync(new IdentityRole("TEST_ROLE")).Result;
            Assert.True(result.Succeeded);
            return FetchTestRole(client, rs);
        }

        [Fact]
        public void AddClaim()
        {
            var client = CreateGraphClient();
            var rs = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityClaim)").Where("n.Type= 'TEST_CLAIM'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();

            var createdRole = CreateRole(client, rs);

            rs.AddClaimAsync(createdRole, new Claim("TEST_CLAIM", "TEST_CLAIM_VALUE")).Wait();
            var res = rs.UpdateAsync(createdRole).Result;
            Assert.True(res.Succeeded);
            var addedRoleClaim = FetchTestRole(client, rs);
            Assert.Single(addedRoleClaim.Claims);
            Assert.Equal(addedRoleClaim.Claims.FirstOrDefault().Type, "TEST_CLAIM");
            Assert.Equal(addedRoleClaim.Claims.FirstOrDefault().Value, "TEST_CLAIM_VALUE");

            // cleanup
            client.Cypher.Match("(n:IdentityClaim)").Where("n.Type= 'TEST_CLAIM'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
        }

        [Fact]
        public void Create()
        {
            var client = CreateGraphClient();
            var rs = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();

            var result = rs.CreateAsync(new IdentityRole("TEST_ROLE")).Result;
            Assert.True(result.Succeeded);
            var fetchedRole = client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'")
                .AsAnnotatedQuery()
                .Return(n => n.As<IdentityRole>())
                .AsCypherQuery().ResultsAsync.Result.FirstOrDefault();
            Assert.NotNull(fetchedRole);
            Assert.NotNull(fetchedRole.Id);
            Assert.NotNull(fetchedRole.Name);
            Assert.NotNull(fetchedRole.ConcurrencyStamp);
            Assert.NotNull(fetchedRole.CreatedOn);
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
        }

        [Fact]
        public void Delete()
        {
            var client = CreateGraphClient();
            var rs = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
            var result = rs.CreateAsync(new IdentityRole("TEST_ROLE")).Result;
            Assert.True(result.Succeeded);
            var fetchedRole = client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'")
                .AsAnnotatedQuery()
                .Return(n => n.As<IdentityRole>())
                .AsCypherQuery().ResultsAsync.Result.FirstOrDefault();
            var res = rs.DeleteAsync(fetchedRole).Result;
            Assert.True(res.Succeeded);
            fetchedRole = client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'")
                .AsAnnotatedQuery()
                .Return(n => n.As<IdentityRole>())
                .AsCypherQuery().ResultsAsync.Result.FirstOrDefault();

            Assert.Null(fetchedRole);

            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
        }

        [Fact]
        public void FindById()
        {
            var client = CreateGraphClient();
            var rs = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
            var result = rs.CreateAsync(new IdentityRole("TEST_ROLE")).Result;
            Assert.True(result.Succeeded);
            var fetchedRole = client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'")
                .AsAnnotatedQuery()
                .Return(n => n.As<IdentityRole>())
                .AsCypherQuery().ResultsAsync.Result.FirstOrDefault();
            var addedRole = rs.FindByIdAsync(fetchedRole.Id).Result;

            Assert.NotNull(addedRole);
            Assert.Equal(addedRole.Id, fetchedRole.Id);
            Assert.Equal(addedRole.Name, fetchedRole.Name);
            Assert.Equal(addedRole.ConcurrencyStamp, fetchedRole.ConcurrencyStamp);
            Assert.Equal(addedRole.CreatedOn.Instant, fetchedRole.CreatedOn.Instant);
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
        }

        [Fact]
        public void RemoveClaim()
        {
            var client = CreateGraphClient();
            var rs = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityClaim)").Where("n.Type= 'TEST_CLAIM'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();

            var createdRole = CreateRole(client, rs);

            rs.AddClaimAsync(createdRole, new Claim("TEST_CLAIM", "TEST_CLAIM_VALUE")).Wait();
            var res = rs.UpdateAsync(createdRole).Result;
            Assert.True(res.Succeeded);
            var addedRoleClaim = FetchTestRole(client, rs);
            Assert.Single(addedRoleClaim.Claims);
            Assert.Equal(addedRoleClaim.Claims.FirstOrDefault().Type, "TEST_CLAIM");
            Assert.Equal(addedRoleClaim.Claims.FirstOrDefault().Value, "TEST_CLAIM_VALUE");

            rs.RemoveClaimAsync(addedRoleClaim, new Claim("TEST_CLAIM", "TEST_CLAIM_VALUE")).Wait();
            res = rs.UpdateAsync(addedRoleClaim).Result;
            Assert.True(res.Succeeded);
            addedRoleClaim = FetchTestRole(client, rs);
            Assert.Empty(addedRoleClaim.Claims);
            // cleanup
            client.Cypher.Match("(n:IdentityClaim)").Where("n.Type= 'TEST_CLAIM'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
        }

        [Fact]
        public void Update()
        {
            var client = CreateGraphClient();
            var rs = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
            var result = rs.CreateAsync(new IdentityRole("TEST_ROLE1")).Result;
            Assert.True(result.Succeeded);
            var fetchedRole = client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE1'")
                .AsAnnotatedQuery()
                .Return(n => n.As<IdentityRole>())
                .AsCypherQuery().ResultsAsync.Result.FirstOrDefault();
            rs.SetRoleNameAsync(fetchedRole, "TEST_ROLE").Wait();
            var res = rs.UpdateAsync(fetchedRole).Result;
            Assert.True(res.Succeeded);
            var fetchedUpdatedRole = client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'")
                .AsAnnotatedQuery()
                .Return(n => n.As<IdentityRole>())
                .AsCypherQuery().ResultsAsync.Result.FirstOrDefault();

            Assert.NotNull(fetchedUpdatedRole);
            Assert.Equal(fetchedUpdatedRole.Id, fetchedRole.Id);
            Assert.Equal(fetchedUpdatedRole.Name, fetchedRole.Name);
            Assert.Equal(fetchedUpdatedRole.ConcurrencyStamp, fetchedRole.ConcurrencyStamp);
            Assert.Equal(fetchedUpdatedRole.CreatedOn.Instant, fetchedRole.CreatedOn.Instant);
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n")
                .ExecuteWithoutResultsAsync().Wait();
        }
    }
}