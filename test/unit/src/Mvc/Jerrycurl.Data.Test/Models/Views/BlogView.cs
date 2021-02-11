using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Data.Test.Models.Views
{
    public class BlogView : Blog
    {
        public IList<BlogPostView> Posts { get; set; }

        public class BlogComment
        {
            [Key("PK_BlogComment")]
            public int Id { get; set; }
            [Ref("PK_BlogPost")]
            public int BlogPostId { get; set; }
            public string Comment { get; set; }
        }

        public class BlogPostView : BlogPost
        {
            public IList<BlogComment> Comments { get; set; }
        }
    }
}
