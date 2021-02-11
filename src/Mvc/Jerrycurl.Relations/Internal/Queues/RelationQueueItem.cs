using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    internal class RelationQueueItem<TList> : NameBuffer
    {
        public TList List { get; }

        public RelationQueueItem(TList list, string namePart, DotNotation notation)
            : base(namePart, notation)
        {
            this.List = list;
        }
    }
}
