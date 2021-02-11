using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Mvc.Internal
{
    public class ProcType
    {
        public static ProcType Command { get; } = null;
        public static ProcType List { get; } = null;
        public static ProcType Aggregate { get; } = null;
    }
}
