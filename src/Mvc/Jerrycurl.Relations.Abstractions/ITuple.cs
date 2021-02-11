using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Relations
{
    public interface ITuple2 : IReadOnlyList<IField2>
    {
        int Degree { get; }
    }
}
