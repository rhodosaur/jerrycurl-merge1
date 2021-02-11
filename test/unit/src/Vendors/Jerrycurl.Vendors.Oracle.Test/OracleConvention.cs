using Jerrycurl.Mvc;
using Jerrycurl.Test;
using Jerrycurl.Test.Extensions;
using Jerrycurl.Test.Profiling;
using Oracle.ManagedDataAccess.Client;

namespace Jerrycurl.Vendors.Oracle.Test
{
    public class OracleConvention : DatabaseConvention
    {
        public override bool Skip => string.IsNullOrEmpty(GetConnectionString());
        public override string SkipReason => "Please configure connection in the 'JERRY_ORACLE_CONN' environment variable.";

        public override void Configure(DomainOptions options)
        {
            options.UseOracle(GetConnectionString());
            options.UseProfiling();
            options.UseNewtonsoftJson();
        }

        public static string GetConnectionString() => GetEnvironmentVariable("JERRY_ORACLE_CONN");
        public static ProfilingConnection GetConnection() => new ProfilingConnection(new OracleConnection(GetConnectionString()));
    }
}
