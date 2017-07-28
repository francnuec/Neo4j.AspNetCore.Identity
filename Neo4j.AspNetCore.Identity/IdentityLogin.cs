using System;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neo4j.AspNetCore.Identity
{
    [Table(Labels.Login)]
    public class IdentityExternalLogin : IEquatable<IdentityExternalLogin>, IEquatable<UserLoginInfo>
    {
        [JsonProperty]
        public string LoginProvider { get; protected internal set; }

        [JsonProperty]
        public string ProviderKey { get; protected internal set; }

        [JsonProperty]
        public string ProviderDisplayName { get; protected internal set; }

        [JsonProperty]
        public Occurrence CreatedOn { get; private set; }

        public IdentityExternalLogin()
        {
            CreatedOn = new Occurrence();
        }

        public IdentityExternalLogin(UserLoginInfo info) : this()
        {
            LoginProvider = info.LoginProvider;
            ProviderKey = info.ProviderKey;
            ProviderDisplayName = info.ProviderDisplayName;
        }

        public UserLoginInfo ToUserLoginInfo()
        {
            return new UserLoginInfo(LoginProvider, ProviderKey, ProviderDisplayName);
        }

        public bool Equals(IdentityExternalLogin other)
        {
            return other.LoginProvider.Equals(LoginProvider)
                && other.ProviderKey.Equals(ProviderKey);
        }

        public bool Equals(UserLoginInfo other)
        {
            return other.LoginProvider.Equals(LoginProvider)
                && other.ProviderKey.Equals(ProviderKey);
        }
    }
}
