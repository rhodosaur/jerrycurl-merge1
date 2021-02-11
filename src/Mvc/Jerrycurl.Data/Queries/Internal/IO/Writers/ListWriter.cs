using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Writers
{
    internal class ListWriter : BaseWriter
    {
        public ListWriter(Node node)
            : base(node)
        {

        }
        public KeyReader PrimaryKey { get; set; }
        public ListIndex Index { get; set; }
        public BaseReader Value { get; set; }
    }
}
