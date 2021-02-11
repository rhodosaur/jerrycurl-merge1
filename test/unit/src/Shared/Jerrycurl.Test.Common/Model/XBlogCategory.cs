using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Test.Model
{
    [Table]
    public class XBlogCategory
    {
        [Id, Key("PK_BlogCategory")]
        public int Id { get; set; }
        [Ref("PK_BlogCategory")]
        public int? ParentId { get; set; }
        public string Name { get; set; }
    }
}
