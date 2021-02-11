using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Data.Test.Models
{
    public class BlogDatabaseModel
    {
        public List<Blog> Blogs { get; set; }
        public List<BlogPost> Posts { get; set; }
        public List<BlogAuthor> Authors { get; set; }
        public List<BlogCategory> Categories { get; set; }
    }
}
