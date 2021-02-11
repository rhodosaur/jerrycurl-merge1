using System;
using System.Collections.Generic;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.Projections
{
    public class ProjectionAttribute : IProjectionAttribute
    {
        public ProjectionIdentity Identity { get; }
        public IProcContext Context { get; }
        public IProjectionMetadata Metadata { get; }
        public IProjectionData Data { get; }
        public ISqlContent Content { get; }

        public ProjectionAttribute(IProjection projection)
        {
            this.Identity = projection.Identity;
            this.Context = projection.Context;
            this.Metadata = projection.Metadata;
            this.Data = projection.Data;
            this.Content = SqlContent.Empty;
        }

        public ProjectionAttribute(ProjectionIdentity identity, IProcContext context, IProjectionMetadata metadata, IProjectionData data)
        {
            this.Identity = identity ?? throw ProjectionException.ArgumentNull(nameof(identity), metadata);
            this.Context = context ?? throw ProjectionException.ArgumentNull(nameof(context), metadata);
            this.Metadata = metadata ?? throw ProjectionException.ArgumentNull(nameof(metadata), metadata);
            this.Data = data;
            this.Content = SqlContent.Empty;
        }

        protected ProjectionAttribute(IProjectionAttribute attribute, IProjectionMetadata metadata, IProjectionData data, ISqlContent content)
        {
            if (attribute == null)
                throw ProjectionException.ArgumentNull(nameof(attribute), metadata);

            this.Context = attribute.Context;
            this.Identity = attribute.Identity;
            this.Metadata = metadata ?? throw ProjectionException.ArgumentNull(nameof(metadata), metadata);
            this.Data = data;
            this.Content = content ?? throw ProjectionException.ArgumentNull(nameof(content), metadata);
        }

        public void WriteTo(ISqlBuffer buffer) => this.Content.WriteTo(buffer);
        public override string ToString() => this.Metadata.Identity.ToString();

        public IProjectionAttribute Append(IEnumerable<IParameter> parameters) => this.With(content: this.Content.Append(parameters));
        public IProjectionAttribute Append(IEnumerable<IUpdateBinding> bindings) => this.With(content: this.Content.Append(bindings));
        public IProjectionAttribute Append(string text) => this.With(content: this.Content.Append(text));
        public IProjectionAttribute Append(params IParameter[] parameter) => this.With(content: this.Content.Append(parameter));
        public IProjectionAttribute Append(params IUpdateBinding[] bindings) => this.With(content: this.Content.Append(bindings));

        public IProjectionAttribute With(IProjectionMetadata metadata = null, IProjectionData data = null, ISqlContent content = null)
        {
            IProjectionMetadata newMetadata = metadata ?? this.Metadata;
            IProjectionData newData = data ?? this.Data;
            ISqlContent newContent = content ?? this.Content;

            return new ProjectionAttribute(this, newMetadata, newData, newContent);
        }
    }
}
