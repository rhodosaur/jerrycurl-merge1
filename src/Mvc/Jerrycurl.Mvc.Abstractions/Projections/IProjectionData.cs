using System;
using System.Collections.Generic;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.Projections
{
    public interface IProjectionData
    {
        public IProjectionMetadata Metadata { get; }
        public IField Value { get; }

        public IProjectionMetadata InputMetadata { get; }
        public IField InputValue { get; }

        public IProjectionMetadata OutputMetadata { get; }
        public IField OutputValue { get; }
    }
}
