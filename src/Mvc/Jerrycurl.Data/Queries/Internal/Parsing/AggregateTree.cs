using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class AggregateTree
    {
        public ISchema Schema { get; set; }
        public NodeBinder Aggregate { get; set; }
    }
}
