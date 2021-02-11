using Jerrycurl.Data.Queries.Internal;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.Compilation;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Jerrycurl.Data.Queries
{
    public sealed class AggregateBuffer<T> : IQueryBuffer
    {
        public ISchema Schema { get; }

        AggregateBuffer IQueryBuffer.Aggregate => this.aggregate;
        ElasticArray IQueryBuffer.Slots => this.slots;

        private AggregateBuffer aggregate;
        private ElasticArray slots;

        public AggregateBuffer(ISchemaStore schemas)
        {
            this.Schema = schemas?.GetSchema(typeof(IList<T>)) ?? throw new ArgumentNullException(nameof(schemas));
            this.InitBuffer();
        }

        private void InitBuffer()
        {
            this.aggregate = new AggregateBuffer(this.Schema);
            this.slots = new ElasticArray();
        }

        public void Insert(IDataReader dataReader)
        {
            BufferWriter writer = QueryCache<T>.GetAggregateWriter(this.Schema, dataReader);

            writer.WriteAll(this, dataReader);
        }

        public async Task InsertAsync(DbDataReader dataReader, CancellationToken cancellationToken = default)
        {
            BufferWriter writer = QueryCache<T>.GetAggregateWriter(this.Schema, dataReader);

            writer.Initialize(this);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                writer.WriteOne(this, dataReader);
        }

        public T Commit()
        {
            try
            {
                QueryCacheKey<AggregateName> cacheKey = this.aggregate.ToCacheKey();
                AggregateReader<T> reader = QueryCache<T>.GetAggregateReader(cacheKey);

                return reader(this);
            }
            finally
            {
                this.InitBuffer();
            }
        }
    }
}
