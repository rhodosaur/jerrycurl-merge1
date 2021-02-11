using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    [DebuggerDisplay("{GetType().Name,nq}: {Metadata,nq}")]
    internal class KeyReader : BaseReader
    {
        public IList<DataReader> Values { get; set; }
        public ParameterExpression List { get; set; }
        public ParameterExpression Array { get; set; }
        public ParameterExpression Variable { get; set; }
        public IReference Reference { get; set; }
        public Type CompositeType { get; set; }
    }
}
