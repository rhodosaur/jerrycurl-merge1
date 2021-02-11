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

namespace Jerrycurl.Data.Queries
{
    public sealed class QueryBuffer : IQueryBuffer
    {
        public ISchemaStore Store => this.Schema.Store;
        public ISchema Schema { get; }
        public QueryType Type { get; }

        AggregateBuffer IQueryBuffer.Aggregate => this.aggregate;
        ElasticArray IQueryBuffer.Slots => this.slots;

        private AggregateBuffer aggregate;
        private ElasticArray slots;

        private Action<IDataReader> innerInsert;
        private Func<DbDataReader, CancellationToken, Task> innerInsertAsync;
        private Func<object> innerCommit;

        public QueryBuffer(ISchema schema, QueryType type)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            this.Type = type;

            this.InitFactories();
            this.Flush();
        }

        private void Flush()
        {
            this.slots = new ElasticArray();

            if (this.Type == QueryType.Aggregate)
                this.aggregate = new AggregateBuffer(this.Schema);
        }

        private void InitFactories()
        {
            switch (this.Type)
            {
                case QueryType.List:
                    this.innerInsert = this.ListInsert;
                    this.innerInsertAsync = this.ListInsertAsync;
                    this.innerCommit = this.ListCommit;
                    break;
                case QueryType.Aggregate:
                    this.innerInsert = this.AggregateInsert;
                    this.innerInsertAsync = this.AggregateInsertAsync;
                    this.innerCommit = this.AggregateCommit;
                    break;
                default:
                    throw QueryException.InvalidQueryType(this.Type);
            }
        }

        public void Insert(IDataReader dataReader) => this.innerInsert(dataReader);
        public Task InsertAsync(DbDataReader dataReader, CancellationToken cancellationToken = default) => this.innerInsertAsync(dataReader, cancellationToken);
        public object Commit() => this.innerCommit();

        #region " Aggregate "
        private void AggregateInsert(IDataReader dataReader)
        {
            BufferWriter writer = QueryCache.GetAggregateWriter(this.Schema, dataReader);

            writer.WriteAll(this, dataReader);
        }

        private async Task AggregateInsertAsync(DbDataReader dataReader, CancellationToken cancellationToken)
        {
            BufferWriter writer = QueryCache.GetAggregateWriter(this.Schema, dataReader);

            writer.Initialize(this);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                writer.WriteOne(this, dataReader);
        }


        private object AggregateCommit()
        {
            try
            {
                QueryCacheKey<AggregateName> cacheKey = this.aggregate.ToCacheKey();
                AggregateReader reader = QueryCache.GetAggregateReader(cacheKey);

                return reader(this);
            }
            finally
            {
                this.Flush();
            }
        }
        #endregion

        #region " List "

        private void ListInsert(IDataReader dataReader)
        {
            BufferWriter writer = QueryCache.GetListWriter(this.Schema, dataReader);

            writer.WriteAll(this, dataReader);
        }

        private async Task ListInsertAsync(DbDataReader dataReader, CancellationToken cancellationToken)
        {
            BufferWriter writer = QueryCache.GetListWriter(this.Schema, dataReader);

            writer.Initialize(this);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                writer.WriteOne(this, dataReader);
        }

        private object ListCommit()
        {
            try
            {
                return this.slots[0];
            }
            finally
            {
                this.Flush();
            }
        }

        #endregion

    }
}
