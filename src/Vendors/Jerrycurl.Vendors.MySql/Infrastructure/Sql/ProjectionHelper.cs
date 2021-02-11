using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc.Sql.MySql
{
    internal static class ProjectionHelper
    {
        public static IJsonMetadata GetJsonMetadata(IProjectionAttribute attribute) => attribute.Metadata.Identity.Lookup<IJsonMetadata>() ??
            throw ProjectionException.FromProjection(attribute, "JSON metadata not found.");
    }
}
