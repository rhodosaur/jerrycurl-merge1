
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations.Internal.Queues;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Compilation
{
    internal class RelationBuffer
    {
        public BufferWriter Writer { get; set; }
        public IField2 Model { get; set; }
        public IField2 Source { get; set; }
        public IRelationQueue[] Queues { get; set; }
        public IField2[] Fields { get; set; }
    }
}
