using Jerrycurl.Data.Queries;
using Jerrycurl.Data.Queries.Internal;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.Compilation;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Jerrycurl.Data.Buf2
{
    public sealed class AggregateBuffer2 : IQueryBuffer
    {
        public ISchemaStore Store => this.Schema.Store;
        public ISchema Schema { get; }

        AggregateBuffer IQueryBuffer.Aggregate => this.aggregate;
        ElasticArray IQueryBuffer.Slots => this.slots;

        private AggregateBuffer aggregate;
        private ElasticArray slots;

        public AggregateBuffer2(ISchema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            this.InitBuffer();
        }

        private void InitBuffer()
        {
            this.aggregate = new AggregateBuffer(this.Schema);
            this.slots = new ElasticArray();
        }

        public void Insert(IDataReader dataReader)
        {
            BufferWriter writer = QueryCache2.GetAggregateWriter(this.Schema, dataReader);

            writer.WriteAll(this, dataReader);
        }

        public async Task InsertAsync(DbDataReader dataReader, CancellationToken cancellationToken = default)
        {
            BufferWriter writer = QueryCache2.GetAggregateWriter(this.Schema, dataReader);

            writer.Initialize(this);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                writer.WriteOne(this, dataReader);
        }

        public object Commit()
        {
            try
            {
                QueryCacheKey<AggregateName> cacheKey = this.aggregate.ToCacheKey();
                AggregateReader reader = QueryCache2.GetAggregateReader(cacheKey);

                return reader(this);
            }
            finally
            {
                this.InitBuffer();
            }
        }
    }
}
