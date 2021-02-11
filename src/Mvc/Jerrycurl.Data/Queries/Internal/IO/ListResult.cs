using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Targets;
using Jerrycurl.Data.Queries.Internal.IO.Writers;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    internal class ListResult : BaseResult
    {
        public QueryType QueryType { get; }
        public List<ListWriter> Lists { get; set; } = new List<ListWriter>();
        public List<TargetWriter> Writers { get; set; } = new List<TargetWriter>();
        public List<BaseTarget> Targets { get; set; } = new List<BaseTarget>();
        public List<ListTarget2> Targets2 { get; set; } = new List<ListTarget2>();
        public List<AggregateWriter> Aggregates { get; set; } = new List<AggregateWriter>();

        public ListResult(ISchema schema, QueryType queryType)
            : base(schema)
        {
            this.QueryType = queryType;
        }
    }
}
