using Jerrycurl.Relations;

namespace Jerrycurl.Data.Test.Model.Blogging
{
    internal class BlogTag
    {
        public int BlogPostId { get; set; }
        public int TagId { get; set; }

        public One<Tag> Tag { get; set; }
    }
}
