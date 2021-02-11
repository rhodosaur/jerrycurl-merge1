using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Filters;
using Jerrycurl.Data.Queries;
using Shouldly;

namespace Jerrycurl.Test
{
    public class MockParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override int Size { get; set; }
        public override string SourceColumn { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override object Value { get; set; }

        public override void ResetDbType() { }
    }
}
