using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc.Projections
{
    public class ProjectionData2 : IProjectionData2
    {
        public IField Value { get; }
        public IField Input { get; }
        public IField Output { get; }

        public ProjectionData2(IField value, IField input, IField output)
        {
            this.Value = value;
            this.Input = input;
            this.Output = output;
        }
    }
}
