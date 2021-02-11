using System.Collections.Generic;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class DynamicReader : BaseReader
    {
        public DynamicReader(Node node)
        {
            this.Metadata = node.Metadata;
            this.Identity = node.Metadata.Identity;
        }

        public IList<BaseReader> Properties { get; set; } = new List<BaseReader>();
    }
}
