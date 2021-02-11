using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Relations;

namespace Jerrycurl.Test.Model
{
    public class XBlogDatabase
    {
        public IList<XBlogCategoryView> Categories { get; set; }
        public IList<XBlogTag> Tags { get; set; }
        public IList<XBlogView> Blogs { get; set; }

        public class XBlogTagView : XBlogTagMap
        {
            public One<XBlogTag> Tag { get; set; }
            public One<XBlog> Blog { get; set; }
        }

        public class XBlogView : XBlog
        {
            public XBlogAuthor Author { get; set; }
            public IList<XBlogPost> Posts { get; set; }
        }

        public class XBlogCategoryView : XBlogCategory
        {
            public IList<XBlogCategoryView> SubCategories { get; set; }
            public IList<XBlogView> Blogs { get; set; }
        }
    }
}
