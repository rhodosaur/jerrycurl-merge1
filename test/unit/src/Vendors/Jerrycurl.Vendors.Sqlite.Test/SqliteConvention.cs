using Jerrycurl.Mvc;
using Jerrycurl.Test;
using Jerrycurl.Test.Extensions;
using Jerrycurl.Test.Profiling;
using Microsoft.Data.Sqlite;

namespace Jerrycurl.Vendors.Sqlite.Test
{
    public class SqliteConvention : DatabaseConvention
    {
        public override void Configure(DomainOptions options)
        {
            options.UseSqlite(GetConnectionString());
            options.UseProfiling();
        }

        public static string GetConnectionString() => "DATA SOURCE=jerry_test.db";
        public static ProfilingConnection GetConnection() => new ProfilingConnection(new SqliteConnection(GetConnectionString()));
    }
}
