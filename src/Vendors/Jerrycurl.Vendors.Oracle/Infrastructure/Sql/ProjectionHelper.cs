using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc.Sql.Oracle
{
    internal static class ProjectionHelper
    {
        public static IJsonMetadata GetJsonMetadata(IProjectionAttribute attribute) => attribute.Metadata.Identity.Lookup<IJsonMetadata>() ??
            throw new ProjectionException($"No JSON information found for {attribute.Metadata.Identity}.");
    }
}
