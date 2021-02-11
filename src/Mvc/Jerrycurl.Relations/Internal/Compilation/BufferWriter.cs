using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations.Internal.Caching;

namespace Jerrycurl.Relations.Internal.Compilation
{
    internal class BufferWriter
    {
        public Action<RelationBuffer> Initializer { get; set; }
        public Action<RelationBuffer>[] Queues { get; set; }
    }
}
