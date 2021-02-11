using System.Collections.Generic;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class NewReader : BaseReader
    {
        public NewReader(IBindingMetadata metadata)
        {
            this.Metadata = metadata;
            this.Identity = metadata.Identity;
        }

        public NewReader(IReferenceMetadata metadata)
            : this(metadata.Identity.Require<IBindingMetadata>())
        {

        }

        public KeyReader PrimaryKey { get; set; }
        public IList<ListIndex> Joins { get; } = new List<ListIndex>();
        public IList<BaseReader> Properties { get; set; } = new List<BaseReader>();
    }
}
