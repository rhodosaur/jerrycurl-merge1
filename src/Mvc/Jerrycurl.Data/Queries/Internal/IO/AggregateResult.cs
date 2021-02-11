using System.Diagnostics;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO
{
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
