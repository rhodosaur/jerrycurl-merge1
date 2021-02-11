using System;
using System.Collections.Generic;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.Projections
{
    public interface IProjection<out TModel> : IProjection
    {
        new IProjection<TModel> Map(Func<IProjectionAttribute, IProjectionAttribute> m);
        new IProjection<TModel> With(IProjectionMetadata metadata = null,
                                     IProjectionData data = null,
                                     IEnumerable<IProjectionAttribute> header = null,
                                     IProjectionOptions options = null);
    }
}