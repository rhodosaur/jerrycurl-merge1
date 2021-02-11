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
    public interface IProjection2 : ISqlWritable
    {
        ProjectionHeader Header { get; }
        IProjectionIdentity Identity { get; }
        IProcContext Context { get; }
        IProjectionOptions Options { get; }

        IProjection2 Append(IEnumerable<IParameter> parameters);
        IProjection2 Append(IEnumerable<IUpdateBinding> bindings);
        IProjection2 Append(string text);
        IProjection2 Append(params IParameter[] parameter);
        IProjection2 Append(params IUpdateBinding[] bindings);

        IProjection2 Map(Func<IProjectionAttribute2, IProjectionAttribute2> map);

        IProjection2 With(ProjectionHeader header = null,
                          IProjectionOptions options = null);
    }
}
