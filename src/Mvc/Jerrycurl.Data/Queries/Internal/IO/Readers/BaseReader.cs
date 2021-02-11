using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    [DebuggerDisplay("{GetType().Name,nq}: {Identity,nq}")]
    internal abstract class BaseReader
    {
        public BaseReader()
        {

        }

        public BaseReader(Node node)
        {
            this.Metadata = node.Metadata;
            this.Identity = node.Identity;
        }

        public BaseReader(IBindingMetadata metadata)
        {
            this.Metadata = metadata;
            this.Identity = metadata.Identity;
        }

        public MetadataIdentity Identity { get; set; }
        public IBindingMetadata Metadata { get; set; }
        public Type KeyType { get; set; }
    }
}
