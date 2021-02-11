using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc
{
    public interface IProcLookup
    {
        string Parameter(ProjectionIdentity identity, IField field);
        string Parameter(ProjectionIdentity identity, MetadataIdentity metadata);
        string Variable(ProjectionIdentity identity, IField field);
        string Table(ProjectionIdentity identity, MetadataIdentity metadata);

        string Custom(string prefix, ProjectionIdentity identity = null, MetadataIdentity metadata = null, IField field = null);
    }
}
