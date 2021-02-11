using System.Data;

namespace Jerrycurl.Data.Queries.Internal.Compilation
{
    internal delegate TItem EnumerateFactory<TItem>(IDataReader dataReader);
}
