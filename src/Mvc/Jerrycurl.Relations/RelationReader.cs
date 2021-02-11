using System;
using System.Collections;
using System.Collections.Generic;
using Jerrycurl.Relations.Internal.Caching;
using Jerrycurl.Relations.Internal.Compilation;
using Jerrycurl.Relations.Internal.Queues;

namespace Jerrycurl.Relations
{
    public class RelationReader : IRelationReader
    {
        public IRelation Relation => this.enumerator.Current;
        public int Degree { get; private set; }
        internal RelationBuffer Buffer { get; private set; }

        int ITuple.Degree => this.Degree;
        int IReadOnlyCollection<IField>.Count => this.Degree;

        private readonly IEnumerator<IRelation> enumerator;
        private int currentIndex;
        private Func<bool> readFactory;
        
        public RelationReader(IEnumerable<IRelation> relations)
        {
            this.enumerator = relations?.GetEnumerator() ?? throw new ArgumentNullException(nameof(relations));
            this.NextResult();
        }

        public RelationReader(IRelation relation)
            : this(new[] { relation })
        {
            
        }

        public bool NextResult()
        {
            if (this.enumerator.MoveNext())
            {
                this.currentIndex = 0;
                this.readFactory = this.ReadFirst;
                this.Degree = this.enumerator.Current.Header.Attributes.Count;

                return true;
            }

            this.readFactory = this.ReadEnd;

            return false;
        }

        public void CopyTo(IField[] target, int sourceIndex, int targetIndex, int length)
            => Array.Copy(this.Buffer.Fields, sourceIndex, target, targetIndex, length);


        public void CopyTo(IField[] target, int length)
            => Array.Copy(this.Buffer.Fields, target, length);

        public IField this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Degree)
                    throw new IndexOutOfRangeException();

                return this.Buffer.Fields[index];
            }
        }

        public void Dispose()
        {
            this.enumerator.Dispose();

            if (this.Buffer == null)
                return;

            for (int i = 0; i < this.Buffer.Queues.Length; i++)
            {
                try
                {
                    if (this.Buffer.Queues[i] is IDisposable disposable)
                        disposable.Dispose();

                    this.Buffer.Queues[i] = null;
                }
                catch { }
            }
        }

        private bool ReadEnd() => false;
        private bool ReadFirst()
        {
            this.Buffer = RelationCache.CreateBuffer(this.Relation);
            this.Buffer.Writer.Initializer(this.Buffer);

            this.currentIndex = 0;

            return (this.readFactory = this.ReadNext)();
        }

        private bool ReadNext()
        {
            Action<RelationBuffer>[] writers = this.Buffer.Writer.Queues;
            IRelationQueue[] queues = this.Buffer.Queues;

            while (this.currentIndex >= 0)
            {
                if (this.currentIndex == writers.Length)
                {
                    this.currentIndex--;

                    return true;
                }
                else if (this.ReadOrThrow(queues[this.currentIndex]))
                {
                    writers[this.currentIndex](this.Buffer);

                    this.currentIndex++;
                }
                else
                    this.currentIndex--;
            }

            return false;
        }

        public bool Read() => this.readFactory();

        private bool ReadOrThrow(IRelationQueue queue)
        {
            try
            {
                return queue.Read();
            }
            catch (Exception ex)
            {
                throw RelationException.CannotForwardQueue(this.Relation, queue, ex);
            }
        }

        public IEnumerator<IField> GetEnumerator()
            => ((IEnumerable<IField>)this.Buffer?.Fields).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();

        public override string ToString() => Tuple.Format(this);
    }
}
