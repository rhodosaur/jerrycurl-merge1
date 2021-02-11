using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.IO.Targets;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class AggregateReader : DataReader
    {
        public AggregateReader(Node node)
            : base(node)
        {

        }

        public AggregateAttribute Attribute { get; set; }
        public BaseReader Value { get; set; }
    }
}
