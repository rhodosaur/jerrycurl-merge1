using System.Diagnostics;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO.Writers
{
    [DebuggerDisplay("{GetType().Name,nq}: {Metadata.Identity,nq}")]
    internal abstract class BaseWriter
    {
        public IBindingMetadata Metadata { get; protected set; }
        public MetadataIdentity Identity { get; protected set; }

        public BaseWriter()
        {

        }
        public BaseWriter(Node node)
        {
            this.Metadata = node.Metadata;
            this.Identity = node.Identity;
        }

        public BaseWriter(IBindingMetadata metadata)
        {
            this.Metadata = metadata;
            this.Identity = metadata.Identity;
        }
    }
}
