using System.Collections.Generic;
using Jerrycurl.Data.Queries.Internal.Caching;

namespace Jerrycurl.Data.Queries.Internal
{
    internal interface IQueryBuffer
    {
        internal List<AggregateAttribute> AggregateHeader { get; }
        internal ElasticArray ListData { get; }
        internal ElasticArray AggregateData { get; }
    }
}
