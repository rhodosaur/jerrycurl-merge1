using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Data.Queries.Internal.IO.Writers;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    internal class EnumerateResult : BaseResult
    {
        public BaseReader Value { get; set; }

        public EnumerateResult(ISchema schema)
            : base(schema)
        {

        }
    }
}
