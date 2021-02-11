using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class EnumerateParser : BaseParser
    {
        public EnumerateParser(ISchema schema)
            : base(schema)
        {
            
        }

        public EnumerateResult Parse(IEnumerable<ColumnAttribute> header)
        {
            NodeTree nodeTree = NodeParser.Parse(this.Schema, header);

            Node valueNode = nodeTree.Items.FirstOrDefault(this.IsResultNode);
            Node listNode = nodeTree.Items.FirstOrDefault(this.IsResultListNode);

            EnumerateResult result = new EnumerateResult(this.Schema);

            result.Value = this.CreateReader(result, listNode ?? valueNode);

            return result;
        }
    }
}
