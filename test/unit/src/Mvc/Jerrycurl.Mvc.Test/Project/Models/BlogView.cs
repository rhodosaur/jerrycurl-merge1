using System.Collections.Generic;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Mvc.Test.Conventions.Models
{
    public class BlogView : Blog
    {
        public IList<BlogPost> Posts { get; set; }
        public int NumberOfPosts { get; set; }
    }
}
