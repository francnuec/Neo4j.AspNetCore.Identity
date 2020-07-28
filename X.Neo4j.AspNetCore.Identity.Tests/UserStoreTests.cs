using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Identity;
using Neo4j.AspNetCore.Identity;
using Neo4jClient;
using Neo4jClient.DataAnnotations;
using Xunit;

namespace X.Neo4j.AspNetCore.Identity.Tests
{
    [Collection("Sequential")]
    public class UserStoreTests
    {
        public static UserStore<IdentityUser> CreateStore(IGraphClient gclient)
        {
            return new UserStore<IdentityUser>(new GraphContext(gclient));
        }
        IdentityUser FetchTestUser(IGraphClient client, UserStore<IdentityUser> us)
        {
            var f = client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'")
                .AsAnnotatedQuery()
                .Return(n => n.As<IdentityUser>())
                .AsCypherQuery().ResultsAsync.Result.FirstOrDefault();
            return us.FindByIdAsync(f.Id, default).Result;
        }

        (IdentityRole, IdentityClaim, IdentityExternalLogin, IdentityUser) CreateTestData(IGraphClient client, UserStore<IdentityUser> us, RoleStore<IdentityRole> rs)
        {
            var user = CreateUser(client, us);
            var role = CreateRole(client, rs);

            us.AddClaimsAsync(user, new[] { new Claim("TEST_CLAIM", "TEST_CLAIM_VALUE") }, default).Wait();
            us.AddLoginAsync(user, new UserLoginInfo("GOOGLE", "KEY", "XGOOGLE"), default).Wait();
            us.AddToRoleAsync(user, role.Name, default).Wait();
            var res = us.UpdateAsync(user, default).Result;
            Assert.True(res.Succeeded);
            user = FetchTestUser(client, us);
            return (role, user.Claims.FirstOrDefault(), user.Logins.FirstOrDefault(), user);
        }

        void Clean(IGraphClient client)
        {
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityClaim)").Where("n.Type= 'TEST_CLAIM'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityExternalLogin)").Where("n.LoginProvider= 'GOOGLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

        }
        IdentityUser CreateUser(IGraphClient client, UserStore<IdentityUser> us)
        {
            var u = new IdentityUser("test@testuser.com", "test@testuser.com");
            us.SetNormalizedEmailAsync(u, "TEST@TESTUSER.COM", default).Wait();
            us.SetNormalizedUserNameAsync(u, "TEST@TESTUSER.COM", default).Wait();

            var result = us.CreateAsync(u, default).Result;
            Assert.True(result.Succeeded);
            return FetchTestUser(client, us);
        }
        public static RoleStore<IdentityRole> CreateRoleStore(IGraphClient gclient)
        {
            return new RoleStore<IdentityRole>(new GraphContext(gclient));
        }
        IdentityRole FetchTestRole(IGraphClient client, RoleStore<IdentityRole> rs)
        {
            var f = client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'")
                .AsAnnotatedQuery()
                .Return(n => n.As<IdentityRole>())
                .AsCypherQuery().ResultsAsync.Result.FirstOrDefault();
            return rs.FindByIdAsync(f.Id).Result;
        }

        IdentityRole CreateRole(IGraphClient client, RoleStore<IdentityRole> rs)
        {
            var role = new IdentityRole("TEST_ROLE");
            rs.SetNormalizedRoleNameAsync(role, "TEST_ROLE").Wait();
            var result = rs.CreateAsync(role).Result;
            Assert.True(result.Succeeded);
            return FetchTestRole(client, rs);
        }
        [Fact]
        public void Create()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

            var u = CreateUser(client, us);
            Assert.NotNull(u);
            Assert.NotNull(u.Id);
            Assert.NotNull(u.UserName);
            Assert.NotNull(u.Email);
            Assert.NotNull(u.CreatedOn);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
        }

        [Fact]
        public void FindById()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

            var user = CreateUser(client, us);
            var u = us.FindByIdAsync(user.Id, default).Result;
            Assert.NotNull(u);
            Assert.NotNull(u.Id);
            Assert.NotNull(u.UserName);
            Assert.NotNull(u.Email);
            Assert.NotNull(u.CreatedOn);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
        }

        [Fact]
        public void FindByEmail()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

            var user = CreateUser(client, us);
            var u = us.FindByEmailAsync(user.Email.NormalizedValue, default).Result;
            Assert.NotNull(u);
            Assert.NotNull(u.Id);
            Assert.NotNull(u.UserName);
            Assert.NotNull(u.Email);
            Assert.NotNull(u.CreatedOn);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
        }
        [Fact]
        public void FindByName()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

            var user = CreateUser(client, us);
            var u = us.FindByNameAsync(user.NormalizedUserName, default).Result;
            Assert.NotNull(u);
            Assert.NotNull(u.Id);
            Assert.NotNull(u.UserName);
            Assert.NotNull(u.Email);
            Assert.NotNull(u.CreatedOn);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
        }
        [Fact]
        public void FindByLogin()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client); var rs = CreateRoleStore(client);

            // cleanup
            Clean(client);
            var data = CreateTestData(client, us, rs);
            var u = us.FindByLoginAsync(data.Item3.LoginProvider, data.Item3.ProviderKey, default).Result;
            Assert.NotNull(u);
            Assert.NotNull(u.Id);
            Assert.NotNull(u.UserName);
            Assert.NotNull(u.Email);
            Assert.NotNull(u.CreatedOn);
            // cleanup
            Clean(client);
        }

        [Fact]
        public void Update()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

            var user = CreateUser(client, us);
            user.SetEmail("test@gomail.com");
            var res = us.UpdateAsync(user, default).Result;
            Assert.True(res.Succeeded);
            user = us.FindByIdAsync(user.Id, default).Result;
            Assert.Equal("test@gomail.com", user.Email.Value);
            user.SetEmail("test@testuser.com");
            res = us.UpdateAsync(user, default).Result;
            Assert.True(res.Succeeded);

            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
        }


        [Fact]
        public void Delete()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

            var user = CreateUser(client, us);

            var res = us.DeleteAsync(user, default).Result;
            Assert.True(res.Succeeded);
            var du = client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'")
                .AsAnnotatedQuery()
                .Return(n => n.As<IdentityUser>())
                .AsCypherQuery().ResultsAsync.Result.FirstOrDefault();
            Assert.Null(du);
            // cleanup
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
        }

        [Fact]
        public void AddToRole()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            var rs = CreateRoleStore(client);
            // cleanup
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

            var user = CreateUser(client, us);
            var role = CreateRole(client, rs);

            us.AddToRoleAsync(user, role.Name, default).Wait();
            us.UpdateAsync(user, CancellationToken.None).Wait();
            var u = us.FindByIdAsync(user.Id, default).Result;
            Assert.Single(u.Roles);
            Assert.Equal(u.Roles.FirstOrDefault().Id, role.Id);

            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
        }
        [Fact]
        public void AddClaim()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            var rs = CreateRoleStore(client);
            // cleanup
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityClaim)").Where("n.Type= 'TEST_CLAIM'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();


            var user = CreateUser(client, us);
            var role = CreateRole(client, rs);

            us.AddClaimsAsync(user, new[] { new Claim("TEST_CLAIM", "TEST_CLAIM_VALUE") }, default).Wait();
            us.UpdateAsync(user, CancellationToken.None).Wait();
            var u = us.FindByIdAsync(user.Id, default).Result;
            Assert.Single(u.Claims);
            Assert.Equal(u.Claims.FirstOrDefault().Type, "TEST_CLAIM");
            Assert.Equal(u.Claims.FirstOrDefault().Value, "TEST_CLAIM_VALUE");

            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityClaim)").Where("n.Type= 'TEST_CLAIM'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

        }
        [Fact]
        public void AddLogin()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            var rs = CreateRoleStore(client);
            // cleanup
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityExternalLogin)").Where("n.LoginProvider= 'GOOGLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();


            var user = CreateUser(client, us);
            var role = CreateRole(client, rs);

            us.AddLoginAsync(user, new UserLoginInfo("GOOGLE", "KEY", "XGOOGLE"), default).Wait();
            us.UpdateAsync(user, CancellationToken.None).Wait();
            var u = us.FindByIdAsync(user.Id, default).Result;
            Assert.Single(u.Logins);
            Assert.Equal(u.Logins.FirstOrDefault().LoginProvider, "GOOGLE");
            Assert.Equal(u.Logins.FirstOrDefault().ProviderKey, "KEY");
            Assert.Equal(u.Logins.FirstOrDefault().ProviderDisplayName, "XGOOGLE");

            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityExternalLogin)").Where("n.LoginProvider= 'GOOGLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

        }

        [Fact]
        public void RemoveLogin()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            var rs = CreateRoleStore(client);
            // cleanup
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityExternalLogin)").Where("n.LoginProvider= 'GOOGLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();


            var user = CreateUser(client, us);
            var role = CreateRole(client, rs);

            us.AddLoginAsync(user, new UserLoginInfo("GOOGLE", "KEY", "XGOOGLE"), default).Wait();
            us.UpdateAsync(user, CancellationToken.None).Wait();
            var u = us.FindByIdAsync(user.Id, default).Result;
            Assert.Single(u.Logins);
            us.RemoveLoginAsync(u, "GOOGLE", "KEY", default);
            us.UpdateAsync(u, CancellationToken.None).Wait();
            u = us.FindByIdAsync(user.Id, default).Result;
            Assert.Empty(u.Logins);

            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityExternalLogin)").Where("n.LoginProvider= 'GOOGLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

        }
        [Fact]
        public void RemoveClaim()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            var rs = CreateRoleStore(client);
            // cleanup
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityClaim)").Where("n.Type= 'TEST_CLAIM'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();


            var user = CreateUser(client, us);
            var role = CreateRole(client, rs);

            us.AddClaimsAsync(user, new[] { new Claim("TEST_CLAIM", "TEST_CLAIM_VALUE") }, default).Wait();
            us.UpdateAsync(user, CancellationToken.None).Wait();
            var u = us.FindByIdAsync(user.Id, default).Result;
            Assert.Single(u.Claims);
            us.RemoveClaimsAsync(u, new[] { new Claim("TEST_CLAIM", "TEST_CLAIM_VALUE") }, default);
            us.UpdateAsync(u, CancellationToken.None).Wait();
            u = us.FindByIdAsync(user.Id, default).Result;
            Assert.Empty(u.Claims);

            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityClaim)").Where("n.Type= 'TEST_CLAIM'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

        }

        [Fact]
        public void RemoveRole()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client);
            var rs = CreateRoleStore(client);
            // cleanup
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

            var user = CreateUser(client, us);
            var role = CreateRole(client, rs);

            us.AddToRoleAsync(user, role.Name, default).Wait();
            us.UpdateAsync(user, CancellationToken.None).Wait();
            var u = us.FindByIdAsync(user.Id, default).Result;
            Assert.Single(u.Roles);
            us.RemoveFromRoleAsync(u, "TEST_ROLE", default);
            us.UpdateAsync(u, CancellationToken.None).Wait();
            u = us.FindByIdAsync(user.Id, default).Result;
            Assert.Empty(u.Roles);
            // cleanup
            client.Cypher.Match("(n:IdentityRole)").Where("n.Name= 'TEST_ROLE'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();
            client.Cypher.Match("(n:IdentityUser)").Where("n.Email_Value= 'test@testuser.com'").DetachDelete("n").ExecuteWithoutResultsAsync().Wait();

        }



        [Fact]
        public void GetUsersInRole()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client); var rs = CreateRoleStore(client);

            // cleanup
            Clean(client);
            var data = CreateTestData(client, us, rs);
            var u = us.GetUsersInRoleAsync(data.Item1.NormalizedName, default).Result;
            Assert.Single(u);

            // cleanup
            Clean(client);
        }
        [Fact]
        public void GetUsersForClaim()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client); var rs = CreateRoleStore(client);

            // cleanup
            Clean(client);
            var data = CreateTestData(client, us, rs);
            var u = us.GetUsersForClaimAsync(new Claim(data.Item2.Type, data.Item2.Value), default).Result;
            Assert.Single(u);

            // cleanup
            Clean(client);
        }

        [Fact]
        public void GeneralPurposeTests()
        {
            var client = RoleStoreTests.CreateGraphClient();
            var us = CreateStore(client); var rs = CreateRoleStore(client);

            // cleanup
            Clean(client);
            var data = CreateTestData(client, us, rs);
            us.SetEmailConfirmedAsync(data.Item4, true, default);
            us.SetLockoutEnabledAsync(data.Item4, true, default);
            us.SetLockoutEndDateAsync(data.Item4, DateTimeOffset.MaxValue, default);
            us.SetPasswordHashAsync(data.Item4, "AAA", default);
            us.SetPhoneNumberAsync(data.Item4, "AAAAAA", default);
            us.SetPhoneNumberConfirmedAsync(data.Item4, true, default);
            us.SetTwoFactorEnabledAsync(data.Item4, true, default);
            var r = us.UpdateAsync(data.Item4, default).Result;
            Assert.True(r.Succeeded);
            // cleanup
            Clean(client);
        }
    }
}
