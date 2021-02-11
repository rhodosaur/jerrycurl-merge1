using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal abstract class DataReader : BaseReader
    {
        public DataReader(Node node)
            : base(node)
        {

        }

        public bool CanBeDbNull { get; set; }
        public ParameterExpression IsDbNull { get; set; }
        public ParameterExpression Variable { get; set; }
    }
}
