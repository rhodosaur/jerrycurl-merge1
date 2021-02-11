using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Test.Model
{
    [Table]
    public class XBlogPost
    {
        [Id, Key("PK_BlogPost")]
        public int Id { get; set; }
        [Ref("PK_Blog")]
        public int BlogId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Headline { get; set; }
        public string Content { get; set; }
    }
}
