using System.Collections.Generic;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.Projections
{
    public interface IProjectionAttribute : ISqlWritable
    {
        ProjectionIdentity Identity { get; }
        IProjectionData Data { get; }
        IProcContext Context { get; }
        ISqlContent Content { get; }

        IProjectionAttribute Append(IEnumerable<IParameter> parameters);
        IProjectionAttribute Append(IEnumerable<IUpdateBinding> bindings);
        IProjectionAttribute Append(string text);
        IProjectionAttribute Append(params IParameter[] parameter);
        IProjectionAttribute Append(params IUpdateBinding[] bindings);

        IProjectionAttribute With(IProjectionMetadata metadata = null,
                                  IProjectionData data = null,
                                  ISqlContent content = null);
    }
}
