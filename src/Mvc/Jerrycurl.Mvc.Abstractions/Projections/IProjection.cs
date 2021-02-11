using System;
using System.Collections.Generic;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.Projections
{
    /// <summary>
    /// Represents an immutable projection buffer comprised of the concatenation of a collection of attributes.
    /// </summary>
    public interface IProjection : ISqlWritable
    {
        ProjectionHeader Header { get; }
        ProjectionIdentity Identity { get; }
        IProcContext Context { get; }
        IProjectionOptions Options { get; }

        IProjection Append(IEnumerable<IParameter> parameters);
        IProjection Append(IEnumerable<IUpdateBinding> bindings);
        IProjection Append(string text);
        IProjection Append(params IParameter[] parameter);
        IProjection Append(params IUpdateBinding[] bindings);

        IProjection Map(Func<IProjectionAttribute, IProjectionAttribute> mapperFunc);

        IProjection With(ProjectionHeader header = null,
                         IProjectionOptions options = null);
    }
}
