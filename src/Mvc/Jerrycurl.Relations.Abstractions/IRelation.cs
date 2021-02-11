using System.Collections.Generic;
using System.Data;

namespace Jerrycurl.Relations
{
    public interface IRelation2
    {
        RelationHeader Header { get; }
        IField2 Source { get; }
        IRelationReader GetReader();
        IDataReader GetDataReader(IEnumerable<string> header);
        IDataReader GetDataReader();

        IEnumerable<ITuple2> Body { get; }
    }
}
