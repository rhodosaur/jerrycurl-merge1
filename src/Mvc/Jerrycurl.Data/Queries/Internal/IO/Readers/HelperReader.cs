using System.Linq.Expressions;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class HelperReader
    {
        public int BufferIndex { get; set; }
        public ParameterExpression Variable { get; set; }
    }
}
