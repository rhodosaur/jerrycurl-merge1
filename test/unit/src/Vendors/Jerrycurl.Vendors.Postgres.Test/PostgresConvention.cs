using Jerrycurl.Mvc;
using Jerrycurl.Test;
using Jerrycurl.Test.Extensions;
using Jerrycurl.Test.Profiling;
using Npgsql;

namespace Jerrycurl.Vendors.Postgres.Test
{
    public class PostgresConvention : DatabaseConvention
    {
        public override bool Skip => string.IsNullOrEmpty(GetConnectionString());
        public override string SkipReason => "Please configure connection in the 'JERRY_POSTGRES_CONN' environment variable.";

        public override void Configure(DomainOptions options)
        {
            options.UsePostgres(GetConnectionString());
            options.UseProfiling();
            options.UseNewtonsoftJson();
        }

        public static string GetConnectionString() => GetEnvironmentVariable("JERRY_POSTGRES_CONN");
        public static ProfilingConnection GetConnection() => new ProfilingConnection(new NpgsqlConnection(GetConnectionString()));
    }
}
