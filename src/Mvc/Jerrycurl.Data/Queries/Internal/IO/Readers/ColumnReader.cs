using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class ColumnReader : DataReader
    {
        public ColumnReader(Node node)
            : base(node)
        {

        }

        public ColumnMetadata Column { get; set; }
        public ParameterExpression Helper { get; set; }
    }
}
