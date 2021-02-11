using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jerrycurl.Diagnostics;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Mvc.V11.Projections
{
    public class ProjectionHeader : IRelationHeader
    {
        private Lazy<HeaderMetadata> innerHeader;

        IReadOnlyList<IRelationMetadata> IRelationHeader.Attributes => this.innerHeader.Value.Relation;
        public IReadOnlyList<IProjectionAttribute2> Attributes => this.innerHeader.Value.Projection;
        public IProjectionAttribute2 Source { get; }
        public ISchema Schema => this.Source.Metadata.Identity.Schema;
        public int Degree => this.Attributes.Count;

        public ProjectionHeader(IProjectionAttribute2 source, IEnumerable<IProjectionAttribute2> attributes)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.innerHeader = new Lazy<HeaderMetadata>(() => this.GetHeaderMetadata(attributes), LazyThreadSafetyMode.None);
        }

        private HeaderMetadata GetHeaderMetadata(IEnumerable<IProjectionAttribute2> attributes)
        {
            List<IProjectionAttribute2> projection = new List<IProjectionAttribute2>();
            List<IRelationMetadata> relation = new List<IRelationMetadata>();

            foreach (IProjectionAttribute2 attribute in attributes)
            {
                projection.Add(attribute);
                relation.Add(attribute.Metadata.Relation);
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
            public IReadOnlyList<IProjectionAttribute2> Projection { get; set; }

        }
    }
}
