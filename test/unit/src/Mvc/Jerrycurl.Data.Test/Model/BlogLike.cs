using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Model
{
    public class BlogLike
    {
        public int BlogPostId { get; set; }
        public DateTime LikedOn { get; set; }
    }
}
