using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neo4j.AspNetCore.Identity
{
    [ComplexType]
    public class FutureOccurrence : Occurrence
    {
        public FutureOccurrence()
        {
        }

        public FutureOccurrence(DateTimeOffset? willOccurOn) : base(willOccurOn)
        {
        }
    }
}