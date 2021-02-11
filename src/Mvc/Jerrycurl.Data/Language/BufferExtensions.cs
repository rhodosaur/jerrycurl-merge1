using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Jerrycurl.Data.Queries;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Language
{
    public static class BufferExtensions
    {
        public static T Commit<T>(this QueryBuffer buffer)
            => (T)buffer.Commit();

        #region " Insert "
        public static void Insert(this QueryBuffer buffer, IRelation relation, params string[] targetHeader)
            => buffer.Insert(relation, (IEnumerable<string>)targetHeader);

        public static void Insert(this QueryBuffer buffer, IRelation relation, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            using IDataReader dataReader = relation.GetDataReader(targetHeader);

            buffer.Insert(dataReader);
        }

        public static void Insert(this QueryBuffer buffer, IRelation relation)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(buffer));

            using IDataReader dataReader = relation.GetDataReader();

            buffer.Insert(dataReader);
        }

        public static void Insert<TSource>(this QueryBuffer buffer, TSource source, params string[] sourceHeader)
            => buffer.Insert(source, (IEnumerable<string>)sourceHeader);

        public static void Insert<TSource>(this QueryBuffer buffer, TSource source, IEnumerable<string> sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            ISchema schema = buffer.Store.GetSchema(typeof(TSource));

            buffer.Insert(source, schema.Select(sourceHeader));
        }

        public static void Insert<TSource>(this QueryBuffer buffer, TSource source, RelationHeader sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = new Relation(buffer.Store.From(source), sourceHeader);

            buffer.Insert(relation);
        }

        public static void Insert<TSource>(this QueryBuffer buffer, TSource source, params (string Source, string Target)[] mappingHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IEnumerable<string> sourceHeader = mappingHeader.Select(t => t.Source);
            IEnumerable<string> targetHeader = mappingHeader.Select(t => t.Target);

            buffer.Insert(source, sourceHeader, targetHeader);
        }

        public static void Insert<TSource>(this QueryBuffer buffer, TSource source, IEnumerable<string> sourceHeader, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = buffer.Store.From(source).Select(sourceHeader);

            buffer.Insert(relation, targetHeader);
        }
        #endregion

        #region " InsertAsync "
        public static Task InsertAsync(this QueryBuffer buffer, IRelation relation, params string[] targetHeader)
            => buffer.InsertAsync(relation, (IEnumerable<string>)targetHeader);

        public static async Task InsertAsync(this QueryBuffer buffer, IRelation relation, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(relation));

            using DbDataReader dataReader = relation.GetDataReader(targetHeader);

            await buffer.InsertAsync(dataReader);
        }

        public static async Task InsertAsync(this QueryBuffer buffer, IRelation relation)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(buffer));

            using DbDataReader dataReader = relation.GetDataReader();

            await buffer.InsertAsync(dataReader);
        }

        public static Task InsertAsync<TSource>(this QueryBuffer buffer, TSource source, params string[] sourceHeader)
            => buffer.InsertAsync(source, (IEnumerable<string>)sourceHeader);

        public static async Task InsertAsync<TSource>(this QueryBuffer buffer, TSource source, IEnumerable<string> sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            ISchema schema = buffer.Store.GetSchema(typeof(TSource));

            await buffer.InsertAsync(source, schema.Select(sourceHeader));
        }

        public static async Task InsertAsync<TSource>(this QueryBuffer buffer, TSource source, RelationHeader sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer == null)
                throw new ArgumentNullException(nameof(sourceHeader));

            IRelation relation = new Relation(buffer.Store.From(source), sourceHeader);

            await buffer.InsertAsync(relation);
        }

        public static async Task InsertAsync<TSource>(this QueryBuffer buffer, TSource source, params (string Source, string Target)[] mappingHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (mappingHeader == null)
                throw new ArgumentNullException(nameof(mappingHeader));

            IEnumerable<string> sourceHeader = mappingHeader.Select(t => t.Source);
            IEnumerable<string> targetHeader = mappingHeader.Select(t => t.Target);

            await buffer.InsertAsync(source, sourceHeader, targetHeader);
        }

        public static async Task InsertAsync<TSource>(this QueryBuffer buffer, TSource source, IEnumerable<string> sourceHeader, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = buffer.Store.From(source).Select(sourceHeader);

            await buffer.InsertAsync(relation, targetHeader);
        }
        #endregion
    }
}
