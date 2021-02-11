using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.Projections
{
    public class ProjectionData : IProjectionData
    {
        public IProjectionMetadata Metadata { get; }
        public IField Value { get; }

        public IProjectionMetadata InputMetadata { get; }
        public IField InputValue { get; }

        public IProjectionMetadata OutputMetadata { get; }
        public IField OutputValue { get; }

        public ProjectionData(IProjectionMetadata metadata, IField value)
        {
            this.Metadata = this.InputMetadata = this.OutputMetadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            this.Value = this.InputValue = this.OutputValue = value;
        }

        public ProjectionData(IProjectionMetadata metadata, IProjectionMetadata inputMetadata, IProjectionMetadata outputMetadata, IField value, IField inputValue, IField outputValue)
        {
            this.Metadata = metadata?? throw new ArgumentNullException(nameof(metadata));
            this.InputMetadata = inputMetadata ?? throw new ArgumentNullException(nameof(inputMetadata));
            this.OutputMetadata = outputMetadata ?? throw new ArgumentNullException(nameof(outputMetadata));
            this.Value = value;
            this.InputValue = inputValue;
            this.OutputValue = outputValue;
        }
    }
}
