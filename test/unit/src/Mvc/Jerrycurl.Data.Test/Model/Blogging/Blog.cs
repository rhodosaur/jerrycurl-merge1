using System;
using System.Collections.Generic;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Model.Blogging
{
    internal class Blog
    {
        [Key("PK_Blog")]
        public int Id { get; set; }
        [Key("PK_Blog_2", IsPrimary = false)]
        public int Id2 { get; set; }
        public string Title { get; set; }
        [Key("PK_Blog_3")]
        public int Id3 { get; set; }

        [Key("PK_Blog_4")]
        public int? Id4 { get; set; }

        [Key("PK_Blog_5", IsPrimary = false)]
        public int? Id5 { get; set; }

        public IList<BlogTag> Tags { get; set; }
        public IList<BlogPost> Posts { get; set; }

        public int GetOnly
        {
            get => 1;
            set => throw new NotSupportedException("NoTryCatchHere");
        }
    }
}
