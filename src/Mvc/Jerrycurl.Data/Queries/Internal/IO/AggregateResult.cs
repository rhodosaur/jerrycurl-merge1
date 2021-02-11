using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Data.Queries.Internal.IO.Targets;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    internal class AggregateResult : BaseResult
    {
        public BaseReader Value { get; set; }
        public AggregateTarget Target { get; set; }
        public IBindingMetadata Metadata { get; set; }

        public AggregateResult(ISchema schema)
            : base(schema)
        {
            this.Metadata = schema.Require<IBindingMetadata>();
        }
    }
}
