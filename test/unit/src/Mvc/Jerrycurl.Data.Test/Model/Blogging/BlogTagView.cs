using Jerrycurl.Relations;

namespace Jerrycurl.Data.Test.Model.Blogging
{
    internal class BlogTagView : BlogTag
    {
        public One<Tag> Tag { get; set; }
        public One<BlogPost> BlogPost { get; set; }
    }
}
