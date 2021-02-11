using System.Data;
using Jerrycurl.Relations;

namespace Jerrycurl.Data.Metadata
{
    internal class BindingParameterInfo : IBindingParameterInfo
    {
        public IBindingMetadata Metadata { get; set; }
        public IDbDataParameter Parameter { get; set; }
        public IField2 Field { get; set; }
    }
}
