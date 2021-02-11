using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Test.Model
{
    [Table]
    public class XBlogTag
    {
        [Key("PK_BlogTag")]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
