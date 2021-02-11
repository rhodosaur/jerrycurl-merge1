using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Metadata;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Data.Queries.Internal.Caching
{
    internal class QueryCacheKey<T> : IEquatable<QueryCacheKey<T>>
        where T : IEquatable<T>
    {
        public ISchema Schema { get; }
        public QueryType Type { get; }
        public IReadOnlyList<T> Items { get; }

        public QueryCacheKey(ISchema schema, QueryType type, IEnumerable<T> items)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            this.Type = type;
            this.Items = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
        }

        public bool Equals(QueryCacheKey<T> other)
        {
            Equality eq = new Equality();

            eq.Add(this.Schema, other?.Schema);
            eq.Add(this.Type, other?.Type);
            eq.AddRange(this.Items, other?.Items);

            return eq.ToEquals();
        }
        public override bool Equals(object obj) => (obj is QueryCacheKey<T> other && this.Equals(other));
        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();

            hashCode.Add(this.Schema);
            hashCode.AddRange(this.Items);

            return hashCode.ToHashCode();
        }
    }
}
