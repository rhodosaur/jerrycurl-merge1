using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    internal class JoinTarget : BaseTarget
    {
        public KeyReader Key { get; set; }
        public ParameterExpression Joins { get; set; }
        public int JoinIndex { get; set; }
        public IReference Reference { get; set; }
    }
}
