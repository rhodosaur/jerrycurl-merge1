using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    internal class JoinIndex
    {
        public ListIndex List { get; set; }
        public KeyReader2 Key { get; set; }
        public ParameterExpression Buffer { get; set; }
        public int BufferIndex { get; set; }
        public IReference Reference { get; set; }
    }
}
