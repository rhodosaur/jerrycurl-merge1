using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal static class TypeHelper
    {
        public static Type GetKeyType(this Type type) => Nullable.GetUnderlyingType(type) ?? type;
    }
}
