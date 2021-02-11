using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Relations;

namespace Jerrycurl.Data.Test.Model.Blogging
{
    internal class BlogPostView : BlogPost
    {
        [One]
        public Blog Blog1 { get; set; }

        public One<Blog> Blog2 { get; set; }
    }
}
