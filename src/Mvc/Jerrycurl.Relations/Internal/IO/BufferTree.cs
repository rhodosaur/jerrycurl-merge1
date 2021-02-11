using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.IO
{
    internal class BufferTree
    {
        public DotNotation Notation { get; set; }
        public SourceReader Source { get; set; }
        public List<QueueReader> Queues { get; } = new List<QueueReader>();
        public List<FieldWriter> Fields { get; } = new List<FieldWriter>();
    }
}
