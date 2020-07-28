using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Neo4j.AspNetCore.Identity
{
    [ComplexType]
    public class UserPhoneNumber : UserContactRecord
    {
        [JsonConstructor]
        protected internal UserPhoneNumber()
        {
        }

        public UserPhoneNumber(string phoneNumber) : base(phoneNumber)
        {
        }
    }
}