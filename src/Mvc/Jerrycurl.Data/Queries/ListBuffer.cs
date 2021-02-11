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
    public sealed class ListBuffer<TItem> : IQueryBuffer
    {
        public ISchema Schema { get; }

        AggregateBuffer IQueryBuffer.Aggregate => null;
        ElasticArray IQueryBuffer.Slots => this.slots;

        private ElasticArray slots;

        public ListBuffer(ISchemaStore schemas)
        {
            this.Schema = schemas?.GetSchema(typeof(IList<TItem>)) ?? throw new ArgumentNullException(nameof(schemas));
            this.InitBuffer();
        }

        private void InitBuffer()
        {
            this.slots = new ElasticArray();
        }

        public void Insert(IDataReader dataReader)
        {
            BufferWriter writer = QueryCache<TItem>.GetListWriter(this.Schema, dataReader);

            writer.WriteAll(this, dataReader);
        }

        public async Task InsertAsync(DbDataReader dataReader, CancellationToken cancellationToken = default)
        {
            BufferWriter writer = QueryCache<TItem>.GetListWriter(this.Schema, dataReader);

            writer.Initialize(this);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                writer.WriteOne(this, dataReader);
        }

        public IList<TItem> Commit()
        {
            try
            {
                return (IList<TItem>)this.slots[0] ?? new List<TItem>();
            }
            finally
            {
                this.InitBuffer();
            }
        }
    }
}
