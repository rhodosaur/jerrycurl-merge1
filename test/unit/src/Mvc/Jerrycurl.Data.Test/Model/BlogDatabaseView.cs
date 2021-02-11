using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations;
using Jerrycurl.Test.Model.Database;

namespace Jerrycurl.Data.Test.Model
{
    public class BlogDatabaseView : Blog
    {
        public One<BlogAuthor> Author { get; set; }
        public One<BlogCategory> Category { get; set; }
        public List<BlogPost> Posts { get; set; }
    }
}
