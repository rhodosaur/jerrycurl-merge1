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
        public IProjectionMetadata InputMetadata { get; }
        public IProjectionMetadata OutputMetadata { get; }
        public IField InputValue { get; }
        public IField OutputValue { get; }

        public ProjectionData(IProjectionMetadata metadata, IField value)
        {
            this.InputMetadata = this.OutputMetadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            this.InputValue = this.OutputValue = value;
        }

        public ProjectionData(IProjectionMetadata inputMetadata, IProjectionMetadata outputMetadata, IField inputValue, IField outputValue)
        {
            this.InputMetadata = inputMetadata ?? throw new ArgumentNullException(nameof(inputMetadata));
            this.OutputMetadata = outputMetadata ?? throw new ArgumentNullException(nameof(outputMetadata));
            this.InputValue = inputValue;
            this.OutputValue = outputValue;
        }
    }
}
