using System.Data;

namespace Jerrycurl.Data.Commands.Internal.Compilation
{
    internal delegate void BufferWriter(IDataReader dataReader, FieldBuffer[] buffers);
}
