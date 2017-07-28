using System;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neo4j.AspNetCore.Identity
{
    [ComplexType]
    public abstract class UserContactRecord : IEquatable<UserEmail>
    {
        [JsonConstructor]
        protected internal UserContactRecord()
        {
            ConfirmationRecord = new ConfirmationOccurrence(null);
        }

        protected UserContactRecord(string value)
            : this()
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        [JsonProperty]
        public string Value { get; protected set; }

        [JsonProperty]
        public ConfirmationOccurrence ConfirmationRecord { get; protected set; }

        public bool IsConfirmed()
        {
            return ConfirmationRecord != null && ConfirmationRecord.Instant != null;
        }

        public void SetConfirmed()
        {
            SetConfirmed(new ConfirmationOccurrence());
        }

        public void SetConfirmed(ConfirmationOccurrence confirmationRecord)
        {
            if (ConfirmationRecord == null || ConfirmationRecord.Instant == null)
            {
                ConfirmationRecord = confirmationRecord;
            }
        }

        public void SetUnconfirmed()
        {
            ConfirmationRecord = new ConfirmationOccurrence(null);
        }

        public bool Equals(UserEmail other)
        {
            return other.Value.Equals(Value);
        }
    }
}
