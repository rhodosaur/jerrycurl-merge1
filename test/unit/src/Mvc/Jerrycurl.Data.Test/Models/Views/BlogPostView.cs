using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Relations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Data.Test.Models.Views
{
    public class BlogPostView : BlogPost
    {
        [One]
        public Blog Blog1 { get; set; }

        public One<Blog> Blog2 { get; set; }
    }
}
