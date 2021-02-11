using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    [DebuggerDisplay("{GetType().Name,nq}: {Metadata,nq}")]
    internal class KeyBinder
    {
        public IEnumerable<ValueBinder> Values { get; set; }
        public ParameterExpression Variable { get; set; }
        public ParameterExpression Slot { get; set; }
        public ParameterExpression Array { get; set; }
        public IReference Metadata { get; set; }
        public Type KeyType { get; set; }
    }
}
