using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Relations;

namespace Jerrycurl.Data.Test.Model.Blogging
{
    internal class BlogTag
    {
        [Ref("PK_BlogPost")]
        public int BlogPostId { get; set; }
        [Ref("PK_Tag")]
        public int TagId { get; set; }
    }
}
