using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    internal class AggregateBinder : ValueBinder
    {
        public AggregateBinder(Node node)
            : base(node)
        {

        }

        public int BufferIndex { get; set; }
        public bool IsPrincipal { get; set; }
    }
}
