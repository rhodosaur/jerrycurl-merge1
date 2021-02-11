using Jerrycurl.Collections;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Extensions;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Data.Queries.Internal.Caching
{
    internal class BufferCache
    {
        private readonly Dictionary<BufferCacheKey, int> parentMap = new Dictionary<BufferCacheKey, int>();
        private readonly Dictionary<int, Dictionary<MetadataIdentity, int>> childMap = new Dictionary<int, Dictionary<MetadataIdentity, int>>();
        private readonly Dictionary<MetadataIdentity, int> aggregateMap = new Dictionary<MetadataIdentity, int>();
        private readonly object state = new object();

        public ISchema Schema { get; }

        public BufferCache(ISchema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public int GetResultIndex() => 0;
        public int GetAggregateIndex(MetadataIdentity metadata)
        {
            lock (this.state)
                return this.aggregateMap.GetOrAdd(metadata, this.aggregateMap.Count);
        }

        public int GetListIndex(MetadataIdentity target) => this.GetParentIndex(new BufferCacheKey(target));
        public int GetParentIndex(IReference reference)
        {
            IEnumerable<Type> key = this.GetParentKeyType(reference);
            BufferCacheKey cacheKey = new BufferCacheKey(key);

            return this.GetParentIndex(cacheKey);
        }

        public int GetChildIndex(IReference reference)
        {
            IReference childReference = reference.Find(ReferenceFlags.Child);
            MetadataIdentity target = childReference.Metadata.Identity;

            int parentIndex = this.GetParentIndex(reference);

            lock (this.state)
            {
                Dictionary<MetadataIdentity, int> innerMap = this.childMap.GetOrAdd(parentIndex);

                return innerMap.GetOrAdd(target, innerMap.Count);
            }
        }

        private IEnumerable<Type> GetParentKeyType(IReference reference)
        {
            IReference parentReference = reference.Find(ReferenceFlags.Parent);

            return parentReference.Key.Properties.Select(m => Nullable.GetUnderlyingType(m.Type) ?? m.Type);
        }

        private int GetParentIndex(BufferCacheKey key)
        {
            lock (this.state)
                return this.parentMap.GetOrAdd(key, this.parentMap.Count + 1);
        }
    }
}
