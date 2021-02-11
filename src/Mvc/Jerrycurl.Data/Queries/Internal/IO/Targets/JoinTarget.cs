using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.IO.Targets
{
    internal class JoinTarget 
    {
        public ListTarget List { get; set; }
        public KeyReader Key { get; set; }
        public ParameterExpression Buffer { get; set; }
        public int Index { get; set; }
    }
}
