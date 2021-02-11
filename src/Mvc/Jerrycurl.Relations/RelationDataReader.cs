using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Relations
{
    internal class RelationDataReader : IDataReader
    {
        public RelationReader InnerReader { get; }
        public IReadOnlyList<string> Header { get; }

        private Dictionary<string, int> headerMap;

        public RelationDataReader(RelationReader innerReader, IEnumerable<string> header)
        {
            this.InnerReader = innerReader ?? throw new ArgumentNullException(nameof(innerReader));
            this.Header = header?.ToList() ?? throw new ArgumentNullException(nameof(header));

            this.InitializeHeader();
        }

        private void InitializeHeader()
        {
            if (this.Header.Count != this.InnerReader.Degree)
                throw RelationException2.InvalidDataReaderHeader(this);

            this.headerMap = new Dictionary<string, int>();

            for (int i = 0; i < this.Header.Count; i++)
            {
                if (string.IsNullOrEmpty(this.Header[i]))
                    throw RelationException2.HeaderCannotBeEmpty(this, i);

                if (this.headerMap.ContainsKey(this.Header[i]))
                    throw RelationException2.HeaderCannotHaveDupes(this, i);

                this.headerMap.Add(this.Header[i], i);
            }
        }

        public int Depth => 0;
        public bool IsClosed => false;
        public int RecordsAffected => 0;
        public int FieldCount => this.InnerReader.Degree;

        public object this[string name] => this[this.GetOrdinal(name)];
        public object this[int i]
        {
            get
            {
                if (this.InnerReader[i].Type == FieldType2.Missing)
                    return DBNull.Value;

                return this.InnerReader[i].Snapshot;
            }
        }

        public void Close() { }
        public bool NextResult() => false;
        public bool Read() => this.InnerReader.Read();

        public string GetDataTypeName(int i) => null;
        public Type GetFieldType(int i) => this.InnerReader.Relation.Header.Attributes[i].Metadata.Type;
        public string GetName(int i) => this.Header[i];
        public int GetOrdinal(string name) => this.headerMap[name];
        public T GetFieldValue<T>(int i)
        {
            if (this.IsDBNull(i))
                throw new InvalidOperationException("Data is null.");

            return (T)this[i];
        }

        public bool IsDBNull(int i) => this.InnerReader[i].Type == FieldType2.Missing;

        public void Dispose() => this.InnerReader.Dispose();

        #region " Get methods "

        public float GetFloat(int i) => this.GetFieldValue<float>(i);
        public Guid GetGuid(int i) => this.GetFieldValue<Guid>(i);
        public short GetInt16(int i) => this.GetFieldValue<short>(i);
        public int GetInt32(int i) => this.GetFieldValue<int>(i);
        public long GetInt64(int i) => this.GetFieldValue<long>(i);
        public string GetString(int i) => this.GetFieldValue<string>(i);
        public object GetValue(int i) => this[i];
        public bool GetBoolean(int i) => this.GetFieldValue<bool>(i);
        public byte GetByte(int i) => this.GetFieldValue<byte>(i);
        public char GetChar(int i) => this.GetFieldValue<char>(i);
        public DateTime GetDateTime(int i) => this.GetFieldValue<DateTime>(i);
        public decimal GetDecimal(int i) => this.GetFieldValue<decimal>(i);
        public double GetDouble(int i) => this.GetFieldValue<double>(i);
        public int GetValues(object[] values)
        {
            int maxLength = Math.Min(values.Length, this.FieldCount);

            for (int i = 0; i < maxLength; i++)
                values[i] = this[i];

            return maxLength;
        }

        #endregion

        #region " Not supported "
        public DataTable GetSchemaTable() => throw new NotSupportedException();
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public IDataReader GetData(int i) => throw new NotSupportedException();
        #endregion
    }
}
