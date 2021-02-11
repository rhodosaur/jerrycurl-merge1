using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Jerrycurl.Data.Buf2;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Queries;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Buf2
{
    public static class RelationExtensions
    {
        public static ISchema Declare<T>(this ISchemaStore store)
            => store.GetSchema(typeof(T));

        public static ListBuffer2 AsListBuffer(this ISchema schema)
            => new ListBuffer2(schema);

        public static ListBuffer2 AsAggregateBuffer(this ISchema schema)
            => new ListBuffer2(schema);
    }
}
