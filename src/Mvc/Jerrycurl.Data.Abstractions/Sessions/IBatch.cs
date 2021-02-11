using System.Data;

namespace Jerrycurl.Data.Sessions
{
    public interface IBatch
    {
        void Build(IDbCommand adoCommand);
    }
}
