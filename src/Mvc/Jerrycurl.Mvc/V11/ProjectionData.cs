using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.V11.Projections;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.V11
{
    public class ProjectionData : IProjectionData
    {
        public IProjectionMetadata Metadata { get; }
        public IField Value { get; }

        public ProjectionData(IProjectionMetadata metadata, IField value)
        {
            this.Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
