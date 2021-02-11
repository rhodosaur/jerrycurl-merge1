using System.Collections.Generic;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class BufferTree
    {
        public ISchema Schema { get; set; }
        public QueryType QueryType { get; set; }
        public List<SlotWriter> Slots { get; set; } = new List<SlotWriter>();
        public List<AggregateWriter> Aggregates { get; set; } = new List<AggregateWriter>();
        public List<ListWriter> Lists { get; set; } = new List<ListWriter>();
        public List<HelperWriter> Helpers { get; set; } = new List<HelperWriter>();
        public List<AggregateName> AggregateNames { get; set; } = new List<AggregateName>();
    }
}
