using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neo4j.AspNetCore.Identity
{
    [ComplexType]
    public class ConfirmationOccurrence : Occurrence
    {
        public ConfirmationOccurrence()
        {
        }

        public ConfirmationOccurrence(DateTimeOffset? confirmedOn) : base(confirmedOn)
        {
        }
    }
}