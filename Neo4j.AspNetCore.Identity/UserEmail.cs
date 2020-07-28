using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Neo4j.AspNetCore.Identity
{
    [ComplexType]
    public class UserEmail : UserContactRecord
    {
        [JsonConstructor]
        protected internal UserEmail()
        {
        }

        public UserEmail(string email) : base(email)
        {
        }

        [JsonProperty] public virtual string NormalizedValue { get; private set; }

        public virtual void SetNormalizedEmail(string normalizedEmail)
        {
            NormalizedValue = normalizedEmail; //?? throw new ArgumentNullException(nameof(normalizedEmail));
        }
    }
}