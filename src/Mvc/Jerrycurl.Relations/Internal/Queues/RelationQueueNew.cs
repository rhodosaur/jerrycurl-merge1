using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    public class RelationQueueNew<TList, TItem> : IRelationQueue
        where TList : IEnumerable<TItem>
    {
        private IEnumerator<TItem> innerEnumerator;
        private IEnumerator<IField[]> cacheEnumerator;

        public TList List => this.CurrentItem != null ? this.CurrentItem.List : default;
        public TItem Current => this.innerEnumerator != null ? this.innerEnumerator.Current : default;
        public RelationQueueItem<TList> CurrentItem { get; set; }
        public int Index => this.CurrentItem?.Index ?? 0;
        public int Depth { get; set; }
        public RelationQueueType Type { get; }
        public IRelationMetadata Metadata { get; }

        private Queue<RelationQueueItem<TList>> innerQueue = new Queue<RelationQueueItem<TList>>();
        private Queue<RelationQueueItem<TList>> innerQueue2 = new Queue<RelationQueueItem<TList>>();

        public RelationQueueNew(IRelationMetadata metadata, RelationQueueType queueType)
        {
            this.Metadata = metadata;
            this.Type = queueType;
            this.Depth = metadata.Depth;
        }

        public IField[] Cache
        {
            get => this.cacheEnumerator?.Current;
            set
            {

            }
        }

        public void Enqueue(RelationQueueItem<TList> item)
        {
            switch (this.Type)
            {
                case RelationQueueType.List:
                    this.innerQueue.Enqueue(item);
                    break;
                case RelationQueueType.Recursive:
                    this.innerQueue.Enqueue(item);                    
                    break;
            }

            this.Start();
        }

        private void Start()
        {
            if (this.innerEnumerator == null)
                this.Dequeue();
        }

        private void Dequeue()
        {
            this.Reset();

            if (this.innerQueue.Count > 0)
            {
                this.CurrentItem = this.innerQueue.Dequeue();
                this.CurrentItem.Reset();

                this.innerEnumerator = this.CurrentItem.List?.GetEnumerator();
            }
        }

        public bool Read()
        {
            if (this.ReadItem())
                return true;

            return false;
        }

        private void EnqueueCached()
        {
            if (this.Type == RelationQueueType.Recursive)
            {
                this.innerQueue = new Queue<RelationQueueItem<TList>>(this.innerQueue2);
                this.innerQueue2.Clear();
                this.Depth++;

                this.Start();
            }
        }

        private bool ReadItem()
        {
            this.Start();

            if (this.innerEnumerator != null && this.innerEnumerator.MoveNext())
            {
                this.CurrentItem.Increment();

                return true;
            }

            this.Reset();

            return false;
        }

        private void Reset()
        {
            this.innerEnumerator?.Dispose();
            this.innerEnumerator = null;
            this.CurrentItem = null;
        }


        public string GetFieldName(string namePart) => this.CurrentItem.CombineWith(namePart);

        public void Dispose() => this.Reset();
    }
}
