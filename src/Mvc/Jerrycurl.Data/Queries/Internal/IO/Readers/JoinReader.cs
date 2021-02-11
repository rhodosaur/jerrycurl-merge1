using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class JoinReader : BaseReader
    {
        public KeyReader JoinKey { get; set; }
        public NewReader List { get; set; }
        public int BufferIndex { get; set; }
        public int JoinIndex { get; set; }
    }
}
