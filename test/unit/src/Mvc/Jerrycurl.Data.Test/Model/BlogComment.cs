using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Data.Test.Model
{
    public class BlogComment
    {
        public int Id { get; set; }
        public int BlogPostId { get; set; }
        public string Author { get; set; }
        public string Comment { get; set; }
    }
}
