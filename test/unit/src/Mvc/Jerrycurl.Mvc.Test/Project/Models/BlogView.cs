using System.Collections.Generic;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Mvc.Test.Conventions.Models
{
    public class BlogView : Blog
    {
        public List<BlogPost> Posts { get; set; }
        public int NumberOfPosts { get; set; }
    }
}
