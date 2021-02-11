namespace Jerrycurl.Relations
{
    public interface IFieldData
    {
        public object Relation { get; }
        public int Index { get; }
        public int Depth { get; }
        public object Parent { get; }
        public object Value { get; }
    }
}
