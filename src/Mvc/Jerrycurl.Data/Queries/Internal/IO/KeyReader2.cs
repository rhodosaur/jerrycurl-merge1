using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    internal class KeyReader2
    {
        public IList<DataReader> Values { get; set; }
    }
}
