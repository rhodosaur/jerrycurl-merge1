using System.Collections.Generic;
using Jerrycurl.Data.Metadata.Annotations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Mvc.Test.Conventions.Models
{
    public class JsonView
    {
        public JsonModel Json { get; set; }

        [Json]
        public class JsonModel
        {
            public int Value { get; set; }
            public ValueModel Model { get; set; }
            public IList<ValueModel> List { get; set; }
        }

        public class ValueModel
        {
            public int Value { get; set; }
        }
    }
}
