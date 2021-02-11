using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.IO.Writers
{
    internal class ListWriter : BaseWriter
    {
        public ListWriter(IBindingMetadata metadata)
            : base(metadata)
        {

        }

        public int BufferIndex { get; set; }
        public ParameterExpression Variable { get; set; }
        public KeyReader JoinKey { get; set; }
        public NewReader Value { get; set; }
    }
}
