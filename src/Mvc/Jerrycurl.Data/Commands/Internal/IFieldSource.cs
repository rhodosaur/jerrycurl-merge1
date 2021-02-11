namespace Jerrycurl.Data.Commands.Internal
{
    internal interface IFieldSource
    {
        object Value { get; }
        bool HasChanged { get; }
    }
}
