using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations
{
    public class RelationAttribute : IEquatable<RelationAttribute>
    {
        public IRelationMetadata Metadata { get; }
        public MetadataIdentity Identity => this.Metadata.Identity;
        public ISchema Schema => this.Identity.Schema;
        public string Name => this.Identity.Name;

        public RelationAttribute(IRelationMetadata metadata)
        {
            this.Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        public RelationAttribute(ISchema schema, string attributeName)
            : this(schema.Require<IRelationMetadata>(attributeName))
        {
            
        }

        public bool Equals(RelationAttribute other) => this.Identity.Equals(other?.Identity);
        public override bool Equals(object obj) => (obj is RelationAttribute other && this.Equals(other));
        public override int GetHashCode() => this.Identity.GetHashCode();
    }
}
