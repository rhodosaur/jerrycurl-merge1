using System;
using System.Collections.Generic;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.V11.Projections
{
    public class ProjectionAttribute2 : IProjectionAttribute2
    {
        public IProjectionIdentity Identity { get; }
        public IProcContext Context { get; }
        public IProjectionMetadata Metadata { get; }
        public IProjectionData Data { get; }
        public ISqlContent Content { get; }

        public ProjectionAttribute2(IProjection2 projection)
        {
            this.Identity = projection.Identity;
            this.Context = projection.Context;
            this.Metadata = projection.Header.Source.Metadata;
            this.Data = projection.Header.Source.Data;
            this.Content = SqlContent.Empty;
        }

        public ProjectionAttribute2(IProjectionIdentity identity, IProcContext context, IProjectionMetadata metadata, IProjectionData data)
        {
            this.Identity = identity ?? throw ProjectionException2.ArgumentNull(nameof(identity), this);
            this.Context = context ?? throw ProjectionException2.ArgumentNull(nameof(context), this);
            this.Metadata = metadata ?? throw ProjectionException2.ArgumentNull(nameof(metadata), this);
            this.Data = data;
            this.Content = SqlContent.Empty;
        }

        protected ProjectionAttribute2(IProjectionAttribute2 attribute, IProjectionMetadata metadata, IProjectionData data, ISqlContent content)
        {
            if (attribute == null)
                throw ProjectionException2.ArgumentNull(nameof(attribute), this);

            this.Context = attribute.Context;
            this.Identity = attribute.Identity;
            this.Metadata = metadata ?? throw ProjectionException2.ArgumentNull(nameof(metadata), this);
            this.Data = data;
            this.Content = content ?? throw ProjectionException2.ArgumentNull(nameof(content), this);
        }

        public void WriteTo(ISqlBuffer buffer) => this.Content.WriteTo(buffer);
        public override string ToString() => this.Metadata.Identity.ToString();

        public IProjectionAttribute2 Append(IEnumerable<IParameter> parameters) => this.With(content: this.Content.Append(parameters));
        public IProjectionAttribute2 Append(IEnumerable<IUpdateBinding> bindings) => this.With(content: this.Content.Append(bindings));
        public IProjectionAttribute2 Append(string text) => this.With(content: this.Content.Append(text));
        public IProjectionAttribute2 Append(params IParameter[] parameter) => this.With(content: this.Content.Append(parameter));
        public IProjectionAttribute2 Append(params IUpdateBinding[] bindings) => this.With(content: this.Content.Append(bindings));

        public IProjectionAttribute2 With(IProjectionMetadata metadata = null, IProjectionData data = null, ISqlContent content = null)
        {
            IProjectionMetadata newMetadata = metadata ?? this.Metadata;
            IProjectionData newData = data ?? this.Data;
            ISqlContent newContent = content ?? this.Content;

            return new ProjectionAttribute2(this, newMetadata, newData, newContent);
        }
    }
}
