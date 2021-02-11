using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Model
{
    public class Blog
    {
        [Key("PK_Blog")]
        public int Id { get; set; }
        public string Title { get; set; }

        public IList<BlogTag> Tags { get; set; }
        public IList<BlogPost> Posts { get; set; }
    }
}
