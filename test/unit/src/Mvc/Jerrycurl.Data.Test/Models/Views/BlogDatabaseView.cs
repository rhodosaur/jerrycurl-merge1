using System.Collections.Generic;
using Jerrycurl.Relations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Data.Test.Models.Views
{
    public class BlogDatabaseView : Blog
    {
        public One<BlogAuthor> Author { get; set; }
        public One<BlogCategory> Category { get; set; }
        public List<BlogPost> Posts { get; set; }
    }
}
