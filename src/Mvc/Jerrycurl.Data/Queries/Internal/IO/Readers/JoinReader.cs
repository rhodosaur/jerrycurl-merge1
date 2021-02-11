using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class JoinReader : BaseReader
    {
        public JoinIndex Index { get; set; }
        public NewReader List { get; set; }
    }
}
