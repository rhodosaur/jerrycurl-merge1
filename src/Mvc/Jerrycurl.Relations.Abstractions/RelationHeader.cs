using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Metadata;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Relations
{
    public class RelationHeader : IEquatable<RelationHeader>
    {
        public ISchema Schema { get; }
        public IReadOnlyList<RelationAttribute> Attributes { get; }

        public RelationHeader(ISchema schema, IReadOnlyList<RelationAttribute> attributes)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            this.Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));

            this.Validate();
        }

        private void Validate()
        {
            for (int i = 0; i < this.Attributes.Count; i++)
            {
                if (this.Attributes[i] == null)
                    throw new InvalidOperationException($"Attribute at index {i} is null.");
                else if (!this.Schema.Equals(this.Attributes[i].Schema))
                    throw new InvalidOperationException($"Attribute at index {i} does not belong to the defining schema.");
            }
        }

        public override string ToString() => $"{this.Schema}({string.Join(", ", this.Attributes.Select(a => $"\"{a.Identity.Name}\""))})";

        public bool Equals(RelationHeader other) => Equality.CombineAll(this.Attributes, other?.Attributes);
        public override bool Equals(object obj) => (obj is RelationHeader other && this.Equals(other));
        public override int GetHashCode() => HashCode.CombineAll(this.Attributes);
    }
}
