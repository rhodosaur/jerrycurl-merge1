using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Relations
{
    public interface IRelationReader : ITuple2, IDisposable
    {
        IRelation2 Relation { get; }
        bool Read();

        void CopyTo(IField2[] target, int sourceIndex, int targetIndex, int length);
        void CopyTo(IField2[] target, int length);

    }
}
