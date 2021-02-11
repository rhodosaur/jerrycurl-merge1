using System.Collections;
using System.Collections.Generic;
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
        public List<JoinTarget> ForeignJoins { get; } = new List<JoinTarget>();
        public ListTarget List { get; set; }
        public JoinTarget Join { get; set; }
        public BaseReader Source { get; set; }
    }
}
