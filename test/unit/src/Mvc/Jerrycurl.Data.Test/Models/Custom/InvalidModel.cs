using System;
using System.Collections.Generic;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Models.Custom
{
    public class InvalidModel
    {
        [Key("PK")]
        public int InvalidId { get; set; }
        public int GetOnly
        {
            get => 1;
            set => throw new NotSupportedException("NoTryCatchHere");
        }

        public IList<RefModel> Many { get; set; }

        public class RefModel
        {
            [Ref("PK")]
            public string RefId { get; set; }
        }
    }
}
