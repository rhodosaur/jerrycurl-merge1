using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Data.Queries.Internal;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries
{
    public interface IQueryBuffer
    {
        ISchemaStore Store { get; }
        ISchema Schema { get; }

        void Insert(IDataReader dataReader);
        Task InsertAsync(DbDataReader dataReader, CancellationToken cancellationToken = default);

        internal AggregateBuffer Aggregate { get; }
        internal ElasticArray Slots { get; }
    }
}
