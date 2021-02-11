using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Model.Custom
{
    public class CompositeModel
    {
        [Key("K", Index = 1)]
        public string Key1 { get; set; }
        [Key("K", Index = 2)]
        public Guid Key2 { get; set; }
        [Key("K", Index = 3)]
        public int Key3 { get; set; }

        public IList<RefModel> Refs { get; set; }

        public class RefModel
        {
            [Ref("K", Index = 3)]
            public int Ref3 { get; set; }
            [Ref("K", Index = 2)]
            public Guid Ref2 { get; set; }
            [Ref("K", Index = 1)]
            public string Ref1 { get; set; }
        }
    }
}
