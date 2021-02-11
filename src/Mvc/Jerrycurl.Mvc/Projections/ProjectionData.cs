using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc.Projections
{
    public class ProjectionData : IProjectionData
    {
        public IField Source { get; }
        public IField Input { get; }
        public IField Output { get; }

        public ProjectionData(IField value)
        {
            this.Source = this.Input = this.Output = value ?? throw new ArgumentNullException(nameof(value));
        }
        public ProjectionData(IField source, IField input, IField output)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Input = input ?? throw new ArgumentNullException(nameof(input));
            this.Output = output ?? throw new ArgumentNullException(nameof(output));
        }

        internal static ProjectionData FromIdentity(ProjectionIdentity identity)
        {
            if (identity.Source != null)
                return new ProjectionData(identity.Source);

            return null;
        }
    }
}
