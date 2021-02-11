using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Vendors.SqlServer.Internal;
#if NET20_BASE
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;
#else
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
#endif

namespace Jerrycurl.Vendors.SqlServer
{
    public class TableValuedParameter : IParameter
    {
        public string Name { get; }
        public IRelation Relation { get; }

        IField IParameter.Source => null;

        public TableValuedParameter(string name, IRelation relation)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Relation = relation ?? throw new ArgumentNullException(nameof(relation));
        }

        public void Build(IDbDataParameter adoParameter)
        {
            SqlParameter sqlParam = adoParameter as SqlParameter ?? throw new InvalidOperationException("Table-valued parameters are only supported on SqlParameter instances.");

            sqlParam.ParameterName = this.Name;

            Action<SqlParameter, IRelation> binder = TvpCache.Binders.GetOrAdd(this.Relation.Header, key =>
            {
                GetHeadingMetadata(key, out IBindingMetadata[] bindingMetadata, out ITableMetadata[] columnMetadata);

                ITableMetadata tableMetadata = columnMetadata[0].HasFlag(TableMetadataFlags.Table) ? columnMetadata[0] : columnMetadata[0].MemberOf;

                string tvpName = string.Join(".", tableMetadata.TableName);
                string[] columnNames = columnMetadata.Select(m => m.ColumnName).ToArray();
                BindingParameterConverter[] converters = bindingMetadata.Select(m => m?.Parameter?.Convert).ToArray();

                return (sp, r) => BindParameter(sp, tvpName, columnNames,  converters, r);
            });

            binder(sqlParam, this.Relation);
        }

        private static void GetHeadingMetadata(RelationHeader header, out IBindingMetadata[] bindingMetadata, out ITableMetadata[] columnMetadata)
        {
            bindingMetadata = new IBindingMetadata[header.Attributes.Count];
            columnMetadata = new ITableMetadata[header.Attributes.Count];

            for (int i = 0; i < header.Attributes.Count; i++)
            {
                IBindingMetadata bindingEntry = header.Attributes[i].Metadata.Identity.Require<IBindingMetadata>();
                ITableMetadata tableEntry = header.Attributes[i].Metadata.Identity.Require<ITableMetadata>();

                bindingMetadata[i] = bindingEntry;
                columnMetadata[i] = tableEntry;
            }

            if (bindingMetadata.Length == 0)
                throw new InvalidOperationException("No columns found.");
        }

        [Obsolete("Use RelationReader?")]
        private static void BindParameter(SqlParameter sqlParam, string tvpName, string[] columnNames, BindingParameterConverter[] converters, IRelation relation)
        {
            ITuple refTuple = relation.Row();

            IEnumerable<SqlDataRecord> iterator()
            {
                SqlDataRecord buffer = CreateSqlBuffer(columnNames, refTuple);

                yield return buffer;

                foreach (ITuple tuple in relation.Body.Skip(1))
                {
                    SetSqlBufferValues(buffer, tuple, converters);

                    yield return buffer;
                }
            }

            if (refTuple == null)
                sqlParam.Value = null;
            else
                sqlParam.Value = iterator();

            sqlParam.SqlDbType = SqlDbType.Structured;
            sqlParam.TypeName = tvpName;
        }

        private static SqlDataRecord CreateSqlBuffer(string[] columnNames, ITuple tuple)
        {
            object[] values = new object[tuple.Degree];
            SqlMetaData[] metadata = new SqlMetaData[tuple.Degree];

            for (int i = 0; i < tuple.Degree; i++)
            {
                Parameter param = new Parameter("P", tuple[i]);
                SqlParameter sqlParam = new SqlParameter();

                param.Build(sqlParam);

                metadata[i] = GetSqlMetadata(columnNames[i], sqlParam);
                values[i] = sqlParam.Value;
            }

            SqlDataRecord dataRecord = new SqlDataRecord(metadata);

            for (int i = 0; i < dataRecord.FieldCount; i++)
                dataRecord.SetValue(i, values[i]);

            return dataRecord;
        }

        [Obsolete("Use Snapshot or Data.Value?")]
        private static void SetSqlBufferValues(SqlDataRecord buffer, ITuple tuple, BindingParameterConverter[] converters)
        {
            for (int i = 0; i < buffer.FieldCount; i++)
            {
                object value = converters[i]?.Invoke(tuple[i].Snapshot) ?? tuple[i].Snapshot;

                buffer.SetValue(i, value);
            }
        }

        private static SqlMetaData GetSqlMetadata(string name, SqlParameter valueParam) => new SqlMetaData(name, valueParam.SqlDbType);
    }
}
