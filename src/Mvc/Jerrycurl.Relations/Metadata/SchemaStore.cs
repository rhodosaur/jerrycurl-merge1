using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jerrycurl.Collections;

namespace Jerrycurl.Relations.Metadata
{
    public class SchemaStore : ISchemaStore
    {
        private readonly ConcurrentDictionary<Type, ISchema> entries = new ConcurrentDictionary<Type, ISchema>();
        private readonly RelationMetadataBuilder relationBuilder = new RelationMetadataBuilder();
        private readonly List<IMetadataBuilder> builders = new List<IMetadataBuilder>();

        public DotNotation Notation { get; }
        internal RelationMetadataBuilder RelationBuilder { get; }
        internal List<IMetadataBuilder> MetadataBuilders { get; }

        IEnumerable<IMetadataBuilder> ISchemaStore.Builders => new IMetadataBuilder[] { this.relationBuilder }.Concat(this.builders);

        public SchemaStore()
            : this(new DotNotation())
        {

        }

        public SchemaStore(DotNotation notation)
        {
            this.Notation = notation ?? throw new ArgumentNullException(nameof(notation));
        }

        public SchemaStore(DotNotation notation, params IMetadataBuilder[] builders)
            : this(notation, (IEnumerable<IMetadataBuilder>)builders)
        {

        }

        public SchemaStore(DotNotation notation, IEnumerable<IMetadataBuilder> builders)
            : this(notation)
        {
            this.builders.AddRange(builders ?? Array.Empty<IMetadataBuilder>());
        }

        public ISchema GetSchema(Type modelType)
        {
            if (modelType == null)
                throw new ArgumentNullException(nameof(modelType));

            return this.entries.GetOrAdd(modelType, this.CreateSchema);
        }

        private Schema CreateSchema(Type modelType)
        {
            Schema schema = new Schema(this);

            IRelationMetadata model = this.relationBuilder.GetModelMetadata(schema, modelType);

            schema.Initialize(model);

            return schema;
        }
    }
}
