using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    internal class ListIndex
    {
        public int BufferIndex { get; set; }
        public ParameterExpression Variable { get; set; }
        public NewReader NewList { get; set; }
        public JoinIndex Join { get; set; }
    }
}
