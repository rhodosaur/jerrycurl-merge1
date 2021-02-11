using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Model.Blogging
{
    internal class BlogComment
    {
        [Key("PK_BlogComment")]
        public int Id { get; set; }
        [Ref("PK_BlogPost")]
        public int BlogPostId { get; set; }
        public string Author { get; set; }
        public string Comment { get; set; }
    }
}
