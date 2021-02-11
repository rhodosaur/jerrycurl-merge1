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

        public KeyReader PrimaryKey { get; set; }
        public ListIndex Index { get; set; }
        public BaseReader Value { get; set; }
    }
}
