using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Data.Queries.Internal;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    internal class RelationQueue<TList, TItem> : IRelationQueue
        where TList : IEnumerable<TItem>
    {
        private IEnumerator<TItem> innerEnumerator;
        private IEnumerator<FieldArray> cacheEnumerator;

        public TList List => this.CurrentItem.List;
        public TItem Current => this.innerEnumerator.Current;
        public RelationQueueItem<TList> CurrentItem => this.innerQueue.Peek();
        public int Index => this.CurrentItem.Index;
        public int Depth { get; set; }
        public RelationQueueType Type { get; }
        public IRelationMetadata Metadata { get; }
        public FieldArray Cache { get; private set; }
        public bool IsCached { get; private set; }

        private Queue<RelationQueueItem<TList>> innerQueue = new Queue<RelationQueueItem<TList>>();

        public RelationQueue(IRelationMetadata metadata, RelationQueueType queueType)
        {
            this.Metadata = metadata;
            this.Type = queueType;
        }

        private void Debug(RelationQueueItem<TList> item, string s)
        {
            if (item.List == null)
                return;

            return;

            Console.WriteLine(s + ": " + string.Join(", ", item.List));
        }
        public void Enqueue(RelationQueueItem<TList> item)
        {
            if (this.IsCached)
            {
                this.IsCached = false;
                this.innerQueue.Clear();
                this.Reset();
            }

            if (this.innerEnumerator == null)
            {
                this.innerQueue.Enqueue(item);

                this.Debug(item, "Enqueue (1)");
            }
                
            else
            {
                this.innerQueue.Enqueue(item);

                this.Debug(item, "Enqueue (2)");
            }

            //if (this.Type == RelationQueueType.Cached)
            //{
            //    this.innerCache.Clear();
            //    this.usingCache = false;
            //    this.Reset();
            //}
        }

        private void Start()
        {
            this.Reset();

            if (this.innerQueue.Count > 0)
            {
                this.Debug(this.CurrentItem, "Start");

                if (this.IsCached)
                    this.cacheEnumerator = this.CurrentItem.Cache.GetEnumerator();
                else
                    this.innerEnumerator = (this.CurrentItem.List ?? (IEnumerable<TItem>)Array.Empty<TItem>()).GetEnumerator();
            }
        }

        private bool IsStarted => this.IsCached ? this.cacheEnumerator != null : this.innerEnumerator != null;

        private bool MoveNext()
        {
            if (!this.IsStarted)
                return false;
            else if (this.IsCached)
                return this.cacheEnumerator.MoveNext();
            else
                return this.innerEnumerator.MoveNext();
        }

        public string GetFieldName(string namePart) => this.CurrentItem.CombineWith(namePart);

        public bool Read()
        {
            while (this.innerQueue.Count > 0)
            {
                if (!this.IsStarted)
                    this.Start();

                if (this.MoveNext())
                {
                    this.CurrentItem.Increment();

                    if (this.IsCached)
                        this.Cache = this.cacheEnumerator.Current;
                    else if (this.Type == RelationQueueType.Cached)
                        this.CurrentItem.Cache.Add(this.Cache = new FieldArray());

                    return true;
                }

                if (this.Type != RelationQueueType.Cached)
                    this.Dequeue();
                else
                {
                    this.IsCached = true;
                    this.Reset();
                    break;
                }
                    

                this.Reset();
            }

            return false;
        }

        private void Reset()
        {
            this.innerEnumerator?.Dispose();
            this.innerEnumerator = null;

            this.Cache = null;
            this.cacheEnumerator?.Dispose();
            this.cacheEnumerator = null;
        }

        private void Dequeue()
        {
            if (this.innerQueue.Count > 0)
                this.innerQueue.Dequeue();
        }

        public void Dispose() => this.Reset();
    }
}
