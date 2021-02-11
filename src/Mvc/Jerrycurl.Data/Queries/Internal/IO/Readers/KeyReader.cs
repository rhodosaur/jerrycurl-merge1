using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class KeyReader : BaseReader
    {
        public IList<DataReader> Values { get; set; }
        public ParameterExpression List { get; set; }
        public ParameterExpression Array { get; set; }
        public ParameterExpression Variable { get; set; }
        public int BufferIndex { get; set; }
        public IReference Reference { get; set; }

        public KeyReader(IBindingMetadata metadata)
            : base(metadata)
        {

        }

        public override string ToString() => $"Key: ({string.Join(", ", this.Values)})";
    }
}
