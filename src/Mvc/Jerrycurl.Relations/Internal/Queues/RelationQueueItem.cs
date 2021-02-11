using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    public class RelationQueueItem<TList> : NameBuffer
    {
        public TList List { get; }
        public List<IField[]> Cache { get; set; }

        public RelationQueueItem(TList list, string namePart, DotNotation notation)
            : base(namePart, notation)
        {
            this.List = list;
        }
    }
}
