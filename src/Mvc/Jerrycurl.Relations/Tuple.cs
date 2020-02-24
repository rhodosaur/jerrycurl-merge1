﻿using Jerrycurl.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Relations
{
    internal class Tuple : ITuple
    {
        private readonly IField[] fields;

        public int Degree { get; }
        public int Count => this.Degree;

        public Tuple(IField[] fields, int degree)
        {
            this.fields = fields;
            this.Degree = degree;
        }

        public IField this[int index]
        {
            get
            {
                if (index < 0)
                    throw new IndexOutOfRangeException("Index must be a non-negative value.");
                else if (index >= this.Degree)
                    throw new IndexOutOfRangeException("Index must be within the degree of the tuple.");

                return this.fields[index];
            }
        }

        public bool Equals(ITuple other) => Equality.CombineAll(this, other);

        public IEnumerator<IField> GetEnumerator()
        {
            for (int i = 0; i < this.Degree; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override bool Equals(object obj) => (obj is ITuple tup && this.Equals(tup));

        public override int GetHashCode() => HashCode.CombineAll(this.fields);

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            s.Append('(');

            foreach (IField field in this)
            {
                string f = field.ToString();

                if (f.Length > 10)
                    f = f.Substring(0, 10);

                s.Append(f.PadRight(10));
                s.Append(',');
            }

            s.Length--;

            s.Append(')');

            return s.ToString();
        }
    }
}
