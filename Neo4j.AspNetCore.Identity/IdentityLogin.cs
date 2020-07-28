using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Neo4j.AspNetCore.Identity
{
    [Table(Labels.Login)]
    public class IdentityExternalLogin : IEquatable<IdentityExternalLogin>, IEquatable<UserLoginInfo>
    {
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

        [JsonProperty] public virtual string LoginProvider { get; protected internal set; }

        [JsonProperty] public virtual string ProviderKey { get; protected internal set; }

        [JsonProperty] public virtual string ProviderDisplayName { get; protected internal set; }

        [JsonProperty] public virtual Occurrence CreatedOn { get; private set; }

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

        public UserLoginInfo ToUserLoginInfo()
        {
            return new UserLoginInfo(LoginProvider, ProviderKey, ProviderDisplayName);
        }
    }
}