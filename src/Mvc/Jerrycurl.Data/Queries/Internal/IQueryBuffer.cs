using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Data.Queries.Internal;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal
{
    internal interface IQueryBuffer
    {
        internal AggregateBuffer Aggregate { get; }
        internal ElasticArray Slots { get; }
    }
}
