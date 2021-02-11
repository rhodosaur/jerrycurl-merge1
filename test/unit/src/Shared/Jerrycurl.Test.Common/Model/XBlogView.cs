using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Test.Model
{
    public class XBlogView : XBlog
    {
        public int NumberOfPosts { get; set; }
        public IList<XBlogPost> Posts { get; set; }
    }
}
