using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Writers
{
    internal class AggregateWriter : BaseWriter
    {
        public AggregateWriter(Node node)
            : base(node)
        {

        }

        public AggregateAttribute Attribute { get; set; }
        public DataReader Value { get; set; }
    }
}
