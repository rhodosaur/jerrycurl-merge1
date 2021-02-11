using System.Linq.Expressions;
using System.Reflection;

namespace Jerrycurl.Data.Queries.Internal.IO.Targets
{
    internal class ListTarget
    {
        public int Index { get; set; }
        public ParameterExpression Variable { get; set; }
        public MethodInfo AddMethod { get; set; }
        public NewExpression NewList { get; set; }
        public NewExpression NewTarget { get; set; }
    }
}
