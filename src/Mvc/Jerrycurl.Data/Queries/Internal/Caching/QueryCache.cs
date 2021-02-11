﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.Compilation;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Parsing;
using Jerrycurl.Relations.Metadata;
using AggregateCacheKey = Jerrycurl.Data.Queries.Internal.Caching.QueryCacheKey<Jerrycurl.Data.Queries.Internal.Caching.AggregateAttribute>;
using ColumnCacheKey = Jerrycurl.Data.Queries.Internal.Caching.QueryCacheKey<Jerrycurl.Data.Queries.Internal.Caching.ColumnAttribute>;

namespace Jerrycurl.Data.Queries.Internal.Caching
{
    internal class QueryCache
    {
        private static readonly ConcurrentDictionary<ColumnCacheKey, object> enumerateMap = new ConcurrentDictionary<ColumnCacheKey, object>();
        private static readonly ConcurrentDictionary<ColumnCacheKey, ListFactory> listMap = new ConcurrentDictionary<ColumnCacheKey, ListFactory>();
        private static readonly ConcurrentDictionary<AggregateCacheKey, object> aggregateMap = new ConcurrentDictionary<AggregateCacheKey, object>();
        private static readonly ConcurrentDictionary<ISchema, BufferCache2> buffers = new ConcurrentDictionary<ISchema, BufferCache2>();

        private static BufferCache2 GetBuffer(ISchema schema) => buffers.GetOrAdd(schema, s => new BufferCache2(s));

        public static AggregateFactory GetAggregateFactory(ISchema schema, IEnumerable<AggregateAttribute> header)
        {
            AggregateCacheKey cacheKey = new AggregateCacheKey(schema, QueryType.Aggregate, header.ToList());

            return (AggregateFactory)aggregateMap.GetOrAdd(cacheKey, k =>
            {
                AggregateParser parser = new AggregateParser(k.Schema);
                AggregateResult result = parser.Parse(k.Header);

                QueryCompiler compiler = new QueryCompiler();

                return compiler.Compile(result);
            });
        }

        public static ListFactory GetListFactory(ISchema schema, QueryType queryType, IDataRecord header)
        {
            ColumnCacheKey cacheKey = GetCacheKey(schema, queryType, header);

            return listMap.GetOrAdd(cacheKey, k =>
            {
                BufferCache2 buffer = GetBuffer(k.Schema);
                ListParser parser = new ListParser(buffer, queryType);
                ListResult result = parser.Parse(k.Header);

                QueryCompiler compiler = new QueryCompiler();

                return compiler.Compile(result);
            });
        }

        public static EnumerateFactory<T> GetEnumerateFactory<T>(ISchema schema, IDataRecord header)
        {
            ColumnCacheKey cacheKey = GetCacheKey(schema, QueryType.List, header);

            return (EnumerateFactory<T>)enumerateMap.GetOrAdd(cacheKey, k =>
            {
                EnumerateParser parser = new EnumerateParser(schema);
                EnumerateResult result = parser.Parse(k.Header);

                QueryCompiler compiler = new QueryCompiler();

                return compiler.Compile<T>(result);
            });
        }

        private static ColumnCacheKey GetCacheKey(ISchema schema, QueryType type, IDataRecord dataRecord)
        {
            int fieldCount = GetFieldCount();
            List<ColumnAttribute> attributes = new List<ColumnAttribute>(fieldCount);

            for (int i = 0; i < fieldCount; i++)
                attributes.Add(GetColumnAttribute(dataRecord, i));

            return new ColumnCacheKey(schema, type, attributes);

            int GetFieldCount()
            {
                try { return dataRecord.FieldCount; }
                catch { return 0; }
            }
        }

        public static ColumnAttribute GetColumnAttribute(IDataRecord dataRecord, int i)
            => new ColumnAttribute(dataRecord.GetName(i), i, dataRecord.GetFieldType(i), dataRecord.GetDataTypeName(i));


    }
}
