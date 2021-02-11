using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.IO.Targets
{
    internal class JoinTarget : BaseTarget
    {
        public KeyReader Key { get; set; }
        public ParameterExpression JoinBuffer { get; set; }
        public int JoinIndex { get; set; }
    }
}
