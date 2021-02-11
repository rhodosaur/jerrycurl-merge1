using System;
using System.Collections;
using System.Collections.Generic;

namespace Jerrycurl.Mvc.Projections
{
    public class ProjectionValues<TItem> : IProjectionValues<TItem>
    {
        public ProjectionIdentity Identity { get; }
        public IProcContext Context { get; }
        public IEnumerable<IProjection<TItem>> Items { get; }

        public ProjectionValues(IProcContext context, ProjectionIdentity identity, IEnumerable<IProjection<TItem>> items)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.Items = items ?? throw new ArgumentNullException(nameof(identity));

            if (this.Items is IProjectionValues<TItem> innerItems)
                this.Items = innerItems;
        }

        public IEnumerator<IProjection<TItem>> GetEnumerator()
        {
            foreach (IProjection<TItem> item in this.Items)
            {
                yield return item;

                this.Context.Execution.Buffer.Mark();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
