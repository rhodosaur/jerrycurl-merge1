using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Test.Model
{
    [Table]
    public class XBlogAuthor
    {
        [Id, Key("PK_BlogAuthor")]
        public int Id { get; set; }
        public string Name { get; set; }
        public string TwitterUrl { get; set; }
    }
}
