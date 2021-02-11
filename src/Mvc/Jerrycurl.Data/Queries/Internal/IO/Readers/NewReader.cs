using System.Collections.Generic;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Targets;
using Jerrycurl.Data.Queries.Internal.Parsing;

namespace Jerrycurl.Data.Queries.Internal.IO.Readers
{
    internal class NewReader : BaseReader
    {
        public KeyReader PrimaryKey { get; set; }
        public IList<JoinTarget> Joins { get; } = new List<JoinTarget>();
        public IList<JoinTarget2> Joins2 { get; } = new List<JoinTarget2>();
        public IList<BaseReader> Properties { get; set; } = new List<BaseReader>();

        public NewReader(IBindingMetadata metadata)
        {
            this.Metadata = metadata;
            this.Identity = metadata.Identity;
        }

        public NewReader(IReferenceMetadata metadata)
            : this(metadata.Identity.Require<IBindingMetadata>())
        {

        }
    }
}
