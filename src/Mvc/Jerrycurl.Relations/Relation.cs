using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Internal.Caching;
using Jerrycurl.Relations.Metadata;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Relations
{
    public class Relation2 : IRelation2
    {
        public RelationHeader Header { get; }
        public IField2 Model => this.Source.Model;
        public IField2 Source { get; }
        IRelationReader IRelation2.GetReader() => this.GetReader();

        public Relation2(IField2 source, RelationHeader header)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Header = header ?? throw new ArgumentNullException(nameof(header));
        }

        public RelationReader GetReader() => new RelationReader(this);
        public IDataReader GetDataReader(IEnumerable<string> header) => new RelationDataReader(this.GetReader(), header);
        public IDataReader GetDataReader() => this.GetDataReader(this.Header.Attributes.Select(a => a.Name));

        public IEnumerable<ITuple2> Body
        {
            get
            {
                using IRelationReader reader = this.GetReader();

                while (reader.Read())
                {
                    IField2[] buffer = new IField2[reader.Degree];

                    reader.CopyTo(buffer, buffer.Length);

                    yield return new Tuple2(buffer);
                }
            }
        }

        public override string ToString() => this.Header.ToString();


        #region " Equality "

        public bool Equals(IField2 other) => Equality.Combine(this.Source, other, m => m.Identity, m => m.Model);
        public override bool Equals(object obj) => (obj is IField2 other && this.Equals(other));
        public override int GetHashCode() => HashCode.Combine(this.Source.Identity, this.Source.Model);

        #endregion
    }
}
