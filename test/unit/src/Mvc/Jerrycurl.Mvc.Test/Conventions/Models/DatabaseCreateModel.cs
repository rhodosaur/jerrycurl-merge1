using System;
using System.Collections.Generic;
using Jerrycurl.Test.Model.Database;

namespace Jerrycurl.Mvc.Test.Conventions.Models
{
    public class DatabaseCreateModel
    {
        public List<BlogView> Blogs { get; set; }

        public class BlogView : Blog
        {
            public BlogAuthor Author { get; set; }
            public List<BlogPostView> Posts { get; set; }
        }

        public class BlogPostView : BlogPost
        {
            public List<BlogTagMap> Tags { get; set; }
        }
    }
}
