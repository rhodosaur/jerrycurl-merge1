using System.Diagnostics;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    [DebuggerDisplay("Aggregate: {Schema,nq}")]
    internal class AggregateResult : BaseResult
    {
        public BaseReader List { get; set; }
        public BaseReader Value { get; set; }

        public AggregateResult(ISchema schema)
            : base(schema)
        {

        }
    }
}
