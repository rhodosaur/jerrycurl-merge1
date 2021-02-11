using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    internal class EnumerateResult : BaseResult
    {
        public BaseReader Value { get; set; }

        public EnumerateResult(ISchema schema)
            : base(schema)
        {

        }
    }
}
