using System.Collections.Generic;
using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Models.Custom
{
    public class NaturalModel
    {
        [Key]
        public int NaturalId { get; set; }
        public List<InnerModel> Many { get; set; }

        public class InnerModel
        {
            [Ref]
            public int NaturalId { get; set; }
        }
    }
}
