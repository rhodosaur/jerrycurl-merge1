using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Relations
{
    internal class ModelData : IFieldData
    {
        public object Relation { get; }
        public int Index => 0;
        public object Parent => null;
        public object Value { get; }

        public ModelData(object value)
        {
            this.Relation = this.Value = value;
        }
    }
}
