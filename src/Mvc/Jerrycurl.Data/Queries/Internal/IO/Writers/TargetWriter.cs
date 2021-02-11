using System.Collections;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Data.Queries.Internal.IO.Targets;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Writers
{
    internal class TargetWriter
    {
        public KeyReader PrimaryKey { get; set; }
        public ListTarget2 List { get; set; }
        public JoinTarget2 Join { get; set; }
        public BaseReader Source { get; set; }
    }
}
