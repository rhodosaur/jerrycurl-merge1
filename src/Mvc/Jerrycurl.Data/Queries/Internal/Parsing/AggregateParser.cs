using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Collections;
using System.Net.Http.Headers;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class AggregateParser : BaseParser
    {
        public AggregateParser(ISchema schema)
            : base(schema)
        {
            
        }

        public AggregateResult Parse(IEnumerable<AggregateAttribute> header)
        {
            NodeTree nodeTree = NodeParser.Parse(this.Schema, header);

            Node valueNode = nodeTree.Items.FirstOrDefault(this.IsResultNode);
            Node listNode = nodeTree.Items.FirstOrDefault(this.IsResultListNode);

            AggregateResult result = new AggregateResult(this.Schema);

            result.Value = this.CreateReader(result, valueNode);
            result.List = this.CreateReader(result, listNode);

            return result;
        }
    }
}
