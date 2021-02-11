using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jerrycurl.Relations;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Language;
using Shouldly;
using Jerrycurl.Test;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Relations.Language;
using System.Data;
using Microsoft.Data.Sqlite;
using Jerrycurl.Test.Profiling;
using System;

namespace Jerrycurl.Data.Test
{
    public class ProfilingTests
    {
        public void Test_Profiled_Connection()
        {
            using var connection = new ProfilingConnection(new SqliteConnection("FILENAME=test.db"));
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";

            IDataReader reader = command.ExecuteReader();
            reader.Read();

            Should.NotThrow(() => reader.GetInt32(0));
            Should.NotThrow(() => reader.IsDBNull(0));

            Should.NotThrow(() => reader.Dispose());

            IDataReader reader2 = command.ExecuteReader();
            reader2.Read();

            Should.NotThrow(() => reader2.GetInt32(0));
            Should.NotThrow(() => reader2.GetInt32(0));

            Should.Throw<InvalidOperationException>(() => reader2.Dispose());

            IDataReader reader3 = command.ExecuteReader();
            reader3.Read();

            Should.NotThrow(() => reader3.IsDBNull(0));
            Should.NotThrow(() => reader3.IsDBNull(0));

            Should.Throw<InvalidOperationException>(() => reader3.Dispose());
        }
    }
}
