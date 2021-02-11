using System.Collections.Generic;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.V11.Projections
{
    public interface IProjectionAttribute2 : ISqlWritable
    {
        IProjectionIdentity Identity { get; }
        IProjectionMetadata Metadata { get; }
        IProjectionData Data { get; }
        IProcContext Context { get; }
        ISqlContent Content { get; }

        IProjectionAttribute2 Append(IEnumerable<IParameter> parameters);
        IProjectionAttribute2 Append(IEnumerable<IUpdateBinding> bindings);
        IProjectionAttribute2 Append(string text);
        IProjectionAttribute2 Append(params IParameter[] parameter);
        IProjectionAttribute2 Append(params IUpdateBinding[] bindings);

        IProjectionAttribute2 With(IProjectionMetadata metadata = null,
                                   IProjectionData data = null,
                                   ISqlContent content = null);
    }
}
