﻿using System.Collections.Generic;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Model
{
    public class BlogPost
    {
        [Key("PK_BlogPost")]
        public int Id { get; set; }
        [Ref("PK_Blog")]
        public int BlogId { get; set; }
        public string Headline { get; set; }
        public string Content { get; set; }

        public IList<BlogComment> Comments { get; set; }
    }
}