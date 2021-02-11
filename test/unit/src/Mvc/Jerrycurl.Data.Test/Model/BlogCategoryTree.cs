using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Relations;

namespace Jerrycurl.Data.Test.Model
{
    internal class BlogCategory
    {
        [Key("PK_BlogCategory")]
        public int Id { get; set; }
        [Ref("PK_BlogCategory")]
        public int? ParentId { get; set; }

        public One<BlogCategory> Parent { get; set; }
        public List<BlogCategory> Children { get; set; }
    }
}
