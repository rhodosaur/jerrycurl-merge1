using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class ListReader : BaseReader
    {
        public ListIndex Index { get; set; }
        public NewReader List { get; set; }
    }
}
