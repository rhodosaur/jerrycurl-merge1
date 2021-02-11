using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.IO.Targets
{
    internal class JoinTarget 
    {
        public ListTarget List { get; set; }
        public MethodInfo AddMethod { get; set; }
        public NewExpression NewList { get; set; }
        public KeyReader Key { get; set; }
        public ParameterExpression Buffer { get; set; }
        public int Index { get; set; }
    }
}
