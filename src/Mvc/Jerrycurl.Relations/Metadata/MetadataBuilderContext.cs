using System;

namespace Jerrycurl.Relations.Metadata
{
    internal class MetadataBuilderContext : IMetadataBuilderContext
    {
        public Schema Schema { get; }
        public RelationMetadata Relation { get; }

        IRelationMetadata IMetadataBuilderContext.Relation => this.Relation;

        public MetadataBuilderContext(Schema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public MetadataBuilderContext(Schema schema, RelationMetadata relation)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            this.Relation = relation ?? throw new ArgumentNullException(nameof(relation));
        }

        public void AddMetadata<TMetadata>(TMetadata metadata) where TMetadata : IMetadata
            => this.Schema.AddMetadata(metadata);

        public TMetadata GetMetadata<TMetadata>(string name) where TMetadata : IMetadata
            => this.Schema.GetMetadataFromCache<TMetadata>(name);
    }
}
