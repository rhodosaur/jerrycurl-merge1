using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Collections;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc.Projections
{
    public class Projection : IProjection
    {
        public ProjectionIdentity Identity { get; }
        public ProjectionHeader Header { get; }
        public IProcContext Context { get; }
        public IProjectionOptions Options { get; }

        public Projection(ProjectionIdentity identity, IProcContext context)
            : this(identity, context, identity.Schema.Require<IProjectionMetadata>())
        {

        }

        internal Projection(ProjectionIdentity identity, IProcContext context, IProjectionMetadata metadata)
        {
            this.Identity = identity ?? throw ProjectionException.ArgumentNull(nameof(identity));
            this.Context = context ?? throw ProjectionException.ArgumentNull(nameof(context));
            this.Options = ProjectionOptions.Default;
            this.Header = this.CreateDefaultHeader(metadata);
        }

        protected Projection(IProjection projection)
        {
            if (projection == null)
                throw ProjectionException.ArgumentNull(nameof(projection));

            this.Identity = projection.Identity;
            this.Context = projection.Context;
            this.Header = projection.Header;
            this.Options = projection.Options;
        }

        protected Projection(IProjection projection, ProjectionHeader header, IProjectionOptions options)
        {
            if (projection == null)
                throw ProjectionException.ArgumentNull(nameof(projection));

            this.Identity = projection.Identity;
            this.Context = projection.Context;
            this.Header = header ?? throw ProjectionException.ArgumentNull(nameof(header));
            this.Options = options ?? throw ProjectionException.ArgumentNull(nameof(options));
        }

        private IEnumerable<IProjectionMetadata> SelectAttributes(IProjectionMetadata metadata)
        {
            if (metadata.HasFlag(RelationMetadataFlags.List) && metadata.Item.HasFlag(TableMetadataFlags.Column))
                return new[] { metadata.Item };
            else if (metadata.HasFlag(RelationMetadataFlags.List) && metadata.Item.HasFlag(TableMetadataFlags.Table))
                return metadata.Item.Properties.Where(a => a.HasFlag(TableMetadataFlags.Column));
            else if (metadata.HasFlag(TableMetadataFlags.Table))
                return metadata.Properties.Where(a => a.HasFlag(TableMetadataFlags.Column));

            return metadata.Properties;
        }

        private ProjectionHeader CreateDefaultHeader(IProjectionMetadata metadata)
        {
            if (this.Header.Source.Data != null)
            {
                ProjectionIdentity identity = this.Identity;
                IProcContext context = this.Context;

                Relation body = new Relation(this.Header.Source.Data.Value, this.Header);

                return new ProjectionHeader(this.Header.Source, headerFactory());

                IEnumerable<ProjectionAttribute> headerFactory()
                {
                    using RelationReader reader = body.GetReader();

                    if (reader.Read())
                    {
                        foreach (var ((metadata, value), index) in this.SelectAttributes(metadata).Zip(reader).Select((e, i) => (e, i)))
                            yield return new ProjectionAttribute(identity, context, metadata, new ProjectionData(metadata.Input, value));
                    }
                }
            }
            else
            {
                ProjectionIdentity identity = this.Identity;
                IProcContext context = this.Context;

                return new ProjectionHeader(this.Header.Source, headerFactory());

                IEnumerable<ProjectionAttribute> headerFactory()
                {
                    foreach (IProjectionMetadata metadata in this.SelectAttributes(metadata))
                        yield return new ProjectionAttribute(identity, context, metadata, data: null);
                }
            }
        }

        public IProjection Map(Func<IProjectionAttribute, IProjectionAttribute> mapperFunc)
        {
            ProjectionIdentity identity = this.Identity;
            IProcContext context = this.Context;
            IEnumerable<IProjectionAttribute> attributes = this.Header.Attributes.Select(mapperFunc);

            return this.With(header: new ProjectionHeader(this.Header.Source, attributes));
        }

        public IProjection Append(IEnumerable<IParameter> parameters) => this.Map(a => a.Append(parameters));
        public IProjection Append(IEnumerable<IUpdateBinding> bindings) => this.Map(a => a.Append(bindings));
        public IProjection Append(string text) => this.Map(a => a.Append(text));
        public IProjection Append(params IParameter[] parameter) => this.Map(a => a.Append(parameter));
        public IProjection Append(params IUpdateBinding[] bindings) => this.Map(a => a.Append(bindings));

        public void WriteTo(ISqlBuffer buffer)
        {
            if (this.Header.Degree > 0)
            {
                this.Header.Attributes[0].WriteTo(buffer);

                foreach (IProjectionAttribute attribute in this.Header.Attributes.Skip(1))
                {
                    buffer.Append(this.Options.Separator);

                    attribute.WriteTo(buffer);
                }
            }
        }

        public IProjection With(ProjectionHeader header = null,
                                IProjectionOptions options = null)
        {
            ProjectionHeader newHeader = header ?? this.Header;
            IProjectionOptions newOptions = options ?? this.Options;

            return new Projection(this, newHeader, newOptions);
        }
    }
}
