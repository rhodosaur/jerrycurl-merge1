using Jerrycurl.Mvc;
using Jerrycurl.Test;
using Jerrycurl.Test.Extensions;
using Jerrycurl.Test.Profiling;
#if NET20_BASE
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace Jerrycurl.Vendors.SqlServer.Test
{
    public class SqlServerConvention : DatabaseConvention
    {
        public override bool Skip => string.IsNullOrEmpty(GetConnectionString());
        public override string SkipReason => "Please configure connection in the 'JERRY_SQLSERVER_CONN' environment variable.";

        public override void Configure(DomainOptions options)
        {
            options.UseSqlServer(GetConnectionString());
            options.UseProfiling();
            options.UseNewtonsoftJson();
        }

        public static string GetConnectionString() => GetEnvironmentVariable("JERRY_SQLSERVER_CONN");
        public static ProfilingConnection GetConnection() => new ProfilingConnection(new SqlConnection(GetConnectionString()));
    }
}
