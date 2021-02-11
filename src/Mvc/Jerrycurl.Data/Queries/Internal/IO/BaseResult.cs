using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Writers;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    [DebuggerDisplay("Enumerate: {Schema,nq}")]
    internal abstract class BaseResult
    {
        public ISchema Schema { get; }
        public List<HelperWriter> Helpers { get; set; } = new List<HelperWriter>();

        public BaseResult(ISchema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }
    }
}
