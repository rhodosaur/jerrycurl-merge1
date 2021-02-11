using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Writers
{
    [DebuggerDisplay("{GetType().Name,nq}: {Metadata.Identity,nq}")]
    internal class HelperWriter : BaseWriter
    {
        public HelperWriter(IBindingMetadata metadata)
            : base(metadata)
        {

        }

        public object Object { get; set; }
        public int BufferIndex { get; set; }
        public ParameterExpression Variable { get; set; }
    }
}
