using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;
using System;

namespace Jerrycurl.Mvc.Projections
{
    public class ProjectionIdentity : IProjectionIdentity
    {
        public IField2 Field { get; }
        public ISchema Schema { get; }

        public ProjectionIdentity(ISchema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public ProjectionIdentity(IField2 field)
        {
            this.Field = field ?? throw new ArgumentNullException(nameof(field));
            this.Schema = field.Metadata.Identity.Schema;

            ProjectionValidator.ValidateIdentity(this);
        }

        public virtual bool Equals(IProjectionIdentity other) => base.Equals(other);
    }
}
