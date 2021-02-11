using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    internal class RelationQueueNew<TList, TItem> : IRelationQueue
        where TList : IEnumerable<TItem>
    {
        private IEnumerator<TItem> innerEnumerator;
        private bool isStarted = false;

        public TList List => this.CurrentItem.List;
        public TItem Current => this.innerEnumerator.Current;
        public RelationQueueItem<TList> CurrentItem => this.innerQueue.Peek();
        public int Index => this.CurrentItem.Index;
        public int Depth => this.Metadata.Depth;
        public RelationQueueType Type { get; }
        public IRelationMetadata Metadata { get; }

        private Queue<RelationQueueItem<TList>> innerQueue = new Queue<RelationQueueItem<TList>>();
        private readonly List<RelationQueueItem<TList>> innerCache = new List<RelationQueueItem<TList>>();

        public RelationQueueNew(IRelationMetadata metadata, RelationQueueType queueType)
        {
            this.Metadata = metadata;
            this.Type = queueType;
        }

        public void Enqueue(RelationQueueItem<TList> item)
        {
            this.innerQueue.Enqueue(item);
        }

        public bool Read()
        {
            while (this.ReadList())
            {
                if (this.ReadItem())
                    return true;
            }

            return false;
        }

        private bool ReadItem()
        {
            if (this.innerEnumerator.MoveNext())
            {
                this.CurrentItem.Increment();

                return true;
            }

            return false;
        }

        private bool ReadList()
        {
            if (this.innerQueue.Count == 0)
                return false;

            this.innerEnumerator?.Dispose();
            this.innerEnumerator = null;

            this.Dequeue();

            if (this.innerQueue.Count == 0 || this.CurrentItem.List == null)
                return false;

            this.innerEnumerator = this.CurrentItem.List.GetEnumerator();

            return true;
        }

        private void Dequeue()
        {
            this.innerQueue.Dequeue();
        }

        public string GetFieldName(string namePart) => this.CurrentItem.CombineWith(namePart);

        public void Dispose()
        {
            this.innerEnumerator?.Dispose();
        }
    }
}
