using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.IO.Initializers
{
    internal class ListInitializer
    {
        public ListInitializer(IBindingMetadata metadata)
        {

        }

        public KeyReader JoinKey { get; set; }
        public NewReader Value { get; set; }
    }
}
