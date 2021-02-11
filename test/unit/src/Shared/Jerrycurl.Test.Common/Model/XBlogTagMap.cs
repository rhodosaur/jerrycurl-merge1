using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Relations;

namespace Jerrycurl.Test.Model
{
    public class XBlogTagMap
    {
        [Ref("PK_BlogPost")]
        public int BlogPostId { get; set; }
        [Ref("PK_Tag")]
        public int TagId { get; set; }
    }
}
