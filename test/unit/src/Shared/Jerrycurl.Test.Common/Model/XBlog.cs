using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Test.Model
{
    [Table]
    public class XBlog
    {
        [Id, Key("PK_Blog")]
        public int Id { get; set; }
        public string Title { get; set; }
        [Ref("PK_BlogAuthor")]
        public int? AuthorId { get; set; }
    }
}
