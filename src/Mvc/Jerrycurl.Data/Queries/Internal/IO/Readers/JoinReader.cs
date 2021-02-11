using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Extensions;
using Jerrycurl.Data.Queries.Internal.IO.Targets;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class JoinReader : BaseReader
    {
        public JoinTarget Target { get; set; }

        public JoinReader(IReference reference)
        {
            IReferenceMetadata metadata = reference.List ?? reference.Find(ReferenceFlags.Child).Metadata;

            this.Metadata = metadata.Identity.Require<IBindingMetadata>();
            this.Identity = this.Metadata.Identity;
        }
    }
}
