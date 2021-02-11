using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations.Internal.Queues;
using Jerrycurl.Relations.Internal.Parsing;

namespace Jerrycurl.Relations.Internal.IO
{
    internal class QueueWriter : NodeWriter
    {
        public QueueIndex Next { get; set; }

        public QueueWriter(Node node)
            : base(node)
        {

        }
    }
}
