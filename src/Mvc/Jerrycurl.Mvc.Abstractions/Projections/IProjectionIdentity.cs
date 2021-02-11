using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;
using System;

namespace Jerrycurl.Mvc.Projections
{
    public interface IProjectionIdentity : IEquatable<IProjectionIdentity>
    {
        IField2 Field { get; }
        ISchema Schema { get; }
    }
}
