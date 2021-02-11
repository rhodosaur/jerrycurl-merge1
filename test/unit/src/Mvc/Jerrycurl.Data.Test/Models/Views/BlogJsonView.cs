using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Data.Test.Models.Views
{
    internal class BlogJsonView
    {
        [Json]
        public Blog Blog { get; set; }
    }
}
