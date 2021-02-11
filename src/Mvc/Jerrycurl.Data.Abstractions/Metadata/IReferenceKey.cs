using System;
using System.Collections.Generic;

namespace Jerrycurl.Data.Metadata
{
    public interface IReferenceKey : IEquatable<IReferenceKey>
    {
        string Name { get; }
        string Other { get; }
        ReferenceKeyFlags Flags { get; }
        IReadOnlyList<IReferenceMetadata> Properties { get; }
    }
}
