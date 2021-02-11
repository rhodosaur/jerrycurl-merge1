using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Writers
{
    [DebuggerDisplay("{GetType().Name,nq}: {Metadata.Identity,nq}")]
    internal class JoinWriter : BaseWriter
    {
        public JoinWriter(Node node)
            : base(node)
        {

        }

        public BaseReader Value { get; set; }
        public ParameterExpression List { get; set; }
        public int BufferIndex { get; set; }
        public int? JoinIndex { get; set; }
        public KeyReader PrimaryKey { get; set; }
        public KeyReader JoinKey { get; set; }
        public int Depth { get; set; }
    }
}
