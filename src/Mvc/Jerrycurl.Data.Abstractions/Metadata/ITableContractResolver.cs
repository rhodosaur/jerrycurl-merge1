namespace Jerrycurl.Data.Metadata
{
    public interface ITableContractResolver
    {
        int Priority { get; }

        string[] GetTableName(ITableMetadata metadata);
        string GetColumnName(ITableMetadata metadata);
    }
}
