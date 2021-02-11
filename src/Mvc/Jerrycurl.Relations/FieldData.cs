using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Relations
{
    internal class FieldData : IFieldData
    {
        public object Relation { get; }
        public int Index { get; }
        public int Depth { get; }
        public object Parent => null;
        public object Value { get; }

        public FieldData(object relation, int index, int depth)
        {
            this.Relation = relation;
            this.Index = index;
            this.Depth = depth;
        }

        public FieldData(object value)
        {
            this.Relation = this.Value = value;
        }
    }
}
