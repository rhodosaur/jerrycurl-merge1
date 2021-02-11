using System;
using System.Collections.Generic;
using Jerrycurl.Data.Queries.Internal;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    public class RelationQueueSexy<TList, TItem> : IRelationQueue
        where TList : IEnumerable<TItem>
    {
        private IEnumerator<TItem> innerEnumerator;
        private IEnumerator<FieldArray> cacheEnumerator;

        public TList List => this.CurrentItem.List;
        public TItem Current => this.innerEnumerator.Current;
        public RelationQueueItem<TList> CurrentItem => this.innerQueue.Peek();
        public int Index => this.CurrentItem.Index;
        public int Depth => this.Metadata.Depth;
        public RelationQueueType Type { get; }
        public IRelationMetadata Metadata { get; }
        public FieldArray Cache { get; private set; }

        private Queue<RelationQueueItem<TList>> innerQueue = new Queue<RelationQueueItem<TList>>();
        private List<RelationQueueItem<TList>> innerCache = new List<RelationQueueItem<TList>>();
        private bool usingCache = false;

        public RelationQueueSexy(IRelationMetadata metadata, RelationQueueType queueType)
        {
            this.Metadata = metadata;
            this.Type = queueType;
        }

        public void Enqueue(RelationQueueItem<TList> item)
        {
            this.innerQueue.Enqueue(item);
        }

        private void Start()
        {
            this.Reset();

            if (this.innerQueue.Count > 0)
            {
                if (this.usingCache)
                    this.cacheEnumerator = this.CurrentItem.Cache.GetEnumerator();
                else
                    this.innerEnumerator = (this.CurrentItem.List ?? (IEnumerable<TItem>)Array.Empty<TItem>()).GetEnumerator();
            }
                
        }

        public string GetFieldName(string namePart) => this.CurrentItem.CombineWith(namePart);

        public bool Read()
        {
            while (this.innerQueue.Count > 0)
            {
                if (this.innerEnumerator == null)
                    this.Start();

                if (this.innerEnumerator != null && this.innerEnumerator.MoveNext())
                {
                    this.CurrentItem.Increment();

                    if (this.usingCache)
                        this.Cache = this.cacheEnumerator.Current;
                    else if (this.Type == RelationQueueType.Cached)
                        this.CurrentItem.Cache.Add(this.Cache = new FieldArray());

                    return true;
                }

                this.Dequeue();
                this.Reset();
            }

            return false;
        }

        private void Reset()
        {
            this.innerEnumerator?.Dispose();
            this.innerEnumerator = null;
            this.Cache = null;
        }

        private void Dequeue()
        {
            if (this.innerQueue.Count > 0)
            {
                RelationQueueItem<TList> item = this.innerQueue.Dequeue();

                if (this.Type == RelationQueueType.Cached)
                    this.innerCache.Add(item);
            }
            else if (this.Type == RelationQueueType.Cached)
            {
                this.innerQueue = new Queue<RelationQueueItem<TList>>(this.innerCache);
                this.usingCache = true;
            }
        }

        public void Dispose() => this.Reset();
    }
}
