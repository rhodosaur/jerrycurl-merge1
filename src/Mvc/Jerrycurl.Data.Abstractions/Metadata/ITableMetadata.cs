using System;
using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Metadata
{
    public interface ITableMetadata : IMetadata
    {
        MetadataIdentity Identity { get; }
        TableMetadataFlags Flags { get; }
        ITableMetadata Owner { get; }
        IReadOnlyList<ITableMetadata> Properties { get; }
        ITableMetadata Item { get; }

        IReadOnlyList<string> TableName { get; }
        string ColumnName { get; }
    }
}
