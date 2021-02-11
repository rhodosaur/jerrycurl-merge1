using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Relations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Data.Test.Models.Views
{
    public class BlogCategoryView : BlogCategory
    {
        public One<BlogCategoryView> Parent { get; set; }
        public List<BlogCategoryView> Children { get; set; }
    }
}
