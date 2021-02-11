using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Data.Queries.Internal;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal
{
    internal interface IQueryBuffer
    {
        internal List<AggregateAttribute> AggregateHeader { get; }
        internal ElasticArray ListData { get; }
        internal ElasticArray AggregateData { get; }
    }
}
