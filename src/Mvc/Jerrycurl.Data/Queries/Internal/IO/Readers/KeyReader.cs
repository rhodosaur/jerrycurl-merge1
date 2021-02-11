using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class KeyReader
    {
        public IList<DataReader> Values { get; set; }
        public IReference Reference { get; set; }
        public IBindingMetadata Target { get; set; }
        public ParameterExpression Variable { get; set; }
        public Type KeyType { get; set; }
    }
}
