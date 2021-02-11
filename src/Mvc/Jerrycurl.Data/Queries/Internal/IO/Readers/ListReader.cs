using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Extensions;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class ListReader : BaseReader
    {
        public ListIndex Index { get; set; }
        public NewReader List { get; set; }

        public ListReader(IReference reference)
        {
            IReferenceMetadata metadata = reference.List ?? reference.Find(ReferenceFlags.Child).Metadata;

            this.Metadata = metadata.Identity.Require<IBindingMetadata>();
            this.Identity = this.Metadata.Identity;
        }
    }
}
