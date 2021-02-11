using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations;

namespace Jerrycurl.Data.Test.Model
{
    public class ListTypeModel
    {
        public IList<int> IList { get; set; }
        public List<int> List { get; set; }
        public IReadOnlyList<int> IReadOnlyList { get; set; }
        public IEnumerable<int> IEnumerable { get; set; }
        public One<int> One { get; set; }
        public IReadOnlyCollection<int> IReadOnlyCollection { get; set; }
    }
}
