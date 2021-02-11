using System.Collections.Generic;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class EnumerateTree
    {
        public ISchema Schema { get; set; }
        public IList<HelperWriter> Helpers { get; } = new List<HelperWriter>();
        public NodeBinder Item { get; set; }
    }
}
