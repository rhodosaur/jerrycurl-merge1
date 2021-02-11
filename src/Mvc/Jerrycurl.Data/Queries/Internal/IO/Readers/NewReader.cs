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

        public KeyReader PrimaryKey { get; set; }
        public IList<KeyReader> JoinKeys { get; } = new List<KeyReader>();
        public IList<BaseReader> Properties { get; set; } = new List<BaseReader>();
    }
}
