using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Data.Queries;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Language
{
    public static class QueryExtensions
    {
        public static IList<T> List<T>(this QueryEngine engine, Query query)
            => engine.List<T>(new[] { query });

        public static IList<T> List<T>(this QueryEngine engine, IEnumerable<Query> queries)
            => engine.Execute<IList<T>>(queries, QueryType.List);

        public static Task<IList<T>> ListAsync<T>(this QueryEngine engine, Query query, CancellationToken cancellationToken = default)
            => engine.ListAsync<T>(new[] { query }, cancellationToken);

        public static Task<IList<T>> ListAsync<T>(this QueryEngine engine, IEnumerable<Query> queries, CancellationToken cancellationToken = default)
            => engine.ExecuteAsync<IList<T>>(queries, QueryType.List, cancellationToken);

        public static T Aggregate<T>(this QueryEngine engine, IEnumerable<Query> queries)
            => engine.Execute<T>(queries, QueryType.Aggregate);

        public static T Aggregate<T>(this QueryEngine engine, Query query)
            => engine.Aggregate<T>(new[] { query });

        public static Task<T> AggregateAsync<T>(this QueryEngine engine, IEnumerable<Query> queries, CancellationToken cancellationToken = default)
            => engine.ExecuteAsync<T>(queries, QueryType.Aggregate, cancellationToken);

        public static Task<T> AggregateAsync<T>(this QueryEngine engine, Query query, CancellationToken cancellationToken = default)
            => engine.AggregateAsync<T>(new[] { query }, cancellationToken);
    }
}
