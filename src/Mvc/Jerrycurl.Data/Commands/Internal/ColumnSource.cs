using System;
using Jerrycurl.Data.Metadata;

namespace Jerrycurl.Data.Commands.Internal
{
    internal class ColumnSource : IFieldSource
    {
        public ColumnMetadata Metadata { get; set; }
        public object Value { get; set; } = DBNull.Value;
        public bool HasChanged => (this.Metadata != null);
    }
}
