using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Model.Blogging
{
    internal class BlogSocial
    {
        [Key("PK_BlogSocial"), Ref("PK_Blog")]
        public int BlogId { get; set; }
        public string TwitterUrl { get; set; }
        public string InstagramUrl { get; set; }
    }
}
