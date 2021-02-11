using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Jerrycurl.Mvc;
using Jerrycurl.Test.Profiling;

namespace Jerrycurl.Test.Extensions
{
    public static class DomainExtensions
    {
        public static void UseProfiling(this DomainOptions options)
        {
            Func<IDbConnection> innerConnection = options.ConnectionFactory;

            options.ConnectionFactory = () => new ProfilingConnection((DbConnection)innerConnection());
        }
    }
}
