using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neo4j.AspNetCore.Identity
{
    [ComplexType]
    public class UserPhoneNumber : UserContactRecord
    {
        [JsonConstructor]
        protected internal UserPhoneNumber() : base() { }

        public UserPhoneNumber(string phoneNumber) : base(phoneNumber)
        {
        }
    }
}
