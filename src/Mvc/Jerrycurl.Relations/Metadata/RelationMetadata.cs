using Jerrycurl.Diagnostics;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Jerrycurl.Relations.Metadata
{
    internal class RelationMetadata : IRelationMetadata
    {
        public MetadataIdentity Identity { get; }

        public IRelationMetadata Parent { get; set; }
        public RelationMetadata MemberOf { get; set; }
        public RelationMetadata Item { get; set; }
        public Lazy<IReadOnlyList<RelationMetadata>> Properties { get; set; }
        public Lazy<IRelationMetadata> Recursor { get; set; }
        public RelationMetadataFlags Flags { get; set; }
        public int Depth { get; set; }

        public IReadOnlyList<Attribute> Annotations { get; set; } = Array.Empty<Attribute>();
        public MemberInfo Member { get; set; }
        public Type Type { get; set; }
        public MethodInfo ReadIndex { get; set; }
        public MethodInfo WriteIndex { get; set; }

        IReadOnlyList<IRelationMetadata> IRelationMetadata.Properties => this.Properties.Value;
        IRelationMetadata IRelationMetadata.Item => this.Item;
        IRelationMetadata IRelationMetadata.Recursor => this.Recursor?.Value;
        IRelationMetadata IRelationMetadata.MemberOf => this.MemberOf;

        public RelationMetadata(MetadataIdentity identity)
        {
            this.Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        public override string ToString() => $"IRelationMetadata: {this.Identity}";
    }
}
