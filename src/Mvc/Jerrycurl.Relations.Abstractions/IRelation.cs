using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Jerrycurl.Relations
{
    public interface IRelation2
    {
        RelationHeader Header { get; }
        IField2 Source { get; }
        IRelationReader GetReader();
        DbDataReader GetDataReader(IEnumerable<string> header);
        DbDataReader GetDataReader();

        IEnumerable<ITuple2> Body { get; }
    }
}
