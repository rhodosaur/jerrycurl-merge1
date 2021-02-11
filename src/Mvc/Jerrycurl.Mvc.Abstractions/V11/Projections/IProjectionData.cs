using System;
using System.Collections.Generic;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.V11.Projections
{
    /// <summary>
    /// Represents an immutable projection buffer comprised of the concatenation of a collection of attributes.
    /// </summary>
    public interface IProjectionData
    {
        public IProjectionMetadata Metadata { get; }
        public IField Value { get; }
    }
}
