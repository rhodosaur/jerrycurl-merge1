using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    [DebuggerDisplay("{GetType().Name,nq}: {Metadata.Identity,nq}")]
    internal class ListWriter
    {
        public ParameterExpression Slot { get; set; }
        public int BufferIndex { get; set; }
        public NodeBinder Item { get; set; }
        public KeyBinder PrimaryKey { get; set; }
        public KeyBinder JoinKey { get; set; }
        public IBindingMetadata Metadata { get; set; }
        public int Depth { get; set; }
        public bool IsUnitList { get; set; }
    }
}
