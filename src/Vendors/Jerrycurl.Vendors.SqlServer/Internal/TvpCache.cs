using Jerrycurl.Relations;
using System;
using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;


namespace Jerrycurl.Vendors.SqlServer.Internal
{
    internal static class TvpCache
    {
        public static ConcurrentDictionary<RelationHeader, Action<SqlParameter, IRelation>> Binders { get; } = new ConcurrentDictionary<RelationHeader, Action<SqlParameter, IRelation>>();
    }
}
