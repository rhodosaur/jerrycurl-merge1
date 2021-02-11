using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jerrycurl.Diagnostics;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Mvc.Projections
{
    public class ProjectionHeader : IRelationHeader
    {
        private Lazy<HeaderMetadata> innerHeader;

        IReadOnlyList<IRelationMetadata> IRelationHeader.Attributes => this.innerHeader.Value.Relation;
        public IReadOnlyList<IProjectionAttribute> Attributes => this.innerHeader.Value.Projection;
        public IProjectionAttribute Source { get; }
        public ISchema Schema => this.Source.Metadata.Identity.Schema;
        public int Degree => this.Attributes.Count;

        public ProjectionHeader(IProjectionAttribute source, IEnumerable<IProjectionAttribute> attributes)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.innerHeader = new Lazy<HeaderMetadata>(() => this.GetHeaderMetadata(attributes), LazyThreadSafetyMode.None);
        }

        private HeaderMetadata GetHeaderMetadata(IEnumerable<IProjectionAttribute> attributes)
        {
            List<IProjectionAttribute> projection = new List<IProjectionAttribute>();
            List<IRelationMetadata> relation = new List<IRelationMetadata>();

            foreach (IProjectionAttribute attribute in attributes)
            {
                projection.Add(attribute);
                relation.Add(attribute.Data.Metadata.Relation);
            }

            return new HeaderMetadata()
            {
                Relation = relation,
                Projection = projection,
            };
        }

        public bool Equals(IRelationHeader other) => Equality.CombineAll(this.innerHeader.Value.Relation, other?.Attributes);
        public override bool Equals(object obj) => (obj is IRelationHeader other && this.Equals(other));
        public override int GetHashCode() => HashCode.CombineAll(this.Attributes);

        private class HeaderMetadata
        {
            public IReadOnlyList<IRelationMetadata> Relation { get; set; }
            public IReadOnlyList<IProjectionAttribute> Projection { get; set; }

        }
    }
}
