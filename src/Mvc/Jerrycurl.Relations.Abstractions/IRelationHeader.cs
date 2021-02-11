using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations
{
    public interface IRelationHeader : IEquatable<IRelationHeader>
    {
        ISchema Schema { get; }
        IReadOnlyList<IRelationMetadata> Attributes { get; }
        int Degree { get; }
    }
}
