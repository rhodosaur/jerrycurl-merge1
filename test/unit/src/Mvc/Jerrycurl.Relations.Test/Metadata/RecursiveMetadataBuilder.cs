using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Test.Metadata
{
    public class RecursiveMetadataBuilder : IMetadataBuilder<CustomMetadata>
    {
        public CustomMetadata GetMetadata(IMetadataBuilderContext context)
            => context.Relation.Identity.Lookup<CustomMetadata>();

        public void Initialize(IMetadataBuilderContext context)
        {

        }
    }
}
