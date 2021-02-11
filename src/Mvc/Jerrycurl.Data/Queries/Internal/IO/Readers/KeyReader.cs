using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class KeyReader
    {
        public IList<DataReader> Values { get; set; }
        public IReference Reference { get; set; }
        public IBindingMetadata Target { get; set; }
        public ParameterExpression Variable { get; set; }
    }
}
