using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Jerrycurl.Data.Queries;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;

namespace Jerrycurl.Data.Language
{
    public static class BufferExtensions
    {
        #region " Insert "
        public static void Insert(this IQueryBuffer buffer, IRelation relation, params string[] insertHeader)
            => buffer.Insert(relation, (IEnumerable<string>)insertHeader);

        public static void Insert(this IQueryBuffer buffer, IRelation relation, IEnumerable<string> insertHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            using IDataReader dataReader = relation.GetDataReader(insertHeader);

            buffer.Insert(dataReader);
        }

        public static void Insert(this IQueryBuffer buffer, IRelation relation)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(buffer));

            using IDataReader dataReader = relation.GetDataReader();

            buffer.Insert(dataReader);
        }

        public static void Insert<TSource>(this IQueryBuffer buffer, IEnumerable<TSource> data, params string[] selectHeader)
            => buffer.Insert(data, (IEnumerable<string>)selectHeader);

        public static void Insert<TSource>(this IQueryBuffer buffer, IEnumerable<TSource> data, IEnumerable<string> selectHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = buffer.Store.From(data).Select(selectHeader);

            buffer.Insert(relation);
        }

        public static void Insert<TSource>(this IQueryBuffer buffer, IEnumerable<TSource> data, params (string Select, string Insert)[] mappingHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IEnumerable<string> selectHeader = mappingHeader.Select(t => t.Select);
            IEnumerable<string> insertHeader = mappingHeader.Select(t => t.Insert);

            buffer.Insert(data, selectHeader, insertHeader);
        }

        public static void Insert<TSource>(this IQueryBuffer buffer, IEnumerable<TSource> data, IEnumerable<string> selectHeader, IEnumerable<string> insertHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = buffer.Store.From(data).Select(selectHeader);

            buffer.Insert(relation, insertHeader);
        }
        #endregion

        #region " InsertAsync "
        public static Task InsertAsync(this IQueryBuffer buffer, IRelation relation, params string[] insertHeader)
            => buffer.InsertAsync(relation, (IEnumerable<string>)insertHeader);

        public static async Task InsertAsync(this IQueryBuffer buffer, IRelation relation, IEnumerable<string> insertHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            using DbDataReader dataReader = relation.GetDataReader(insertHeader);

            await buffer.InsertAsync(dataReader);
        }

        public static async Task InsertAsync(this IQueryBuffer buffer, IRelation relation)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(buffer));

            using DbDataReader dataReader = relation.GetDataReader();

            await buffer.InsertAsync(dataReader);
        }

        public static Task InsertAsync<TSource>(this IQueryBuffer buffer, IEnumerable<TSource> data, params string[] selectHeader)
            => buffer.InsertAsync(data, (IEnumerable<string>)selectHeader);

        public static async Task InsertAsync<TSource>(this IQueryBuffer buffer, IEnumerable<TSource> data, IEnumerable<string> selectHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = buffer.Store.From(data).Select(selectHeader);

            await buffer.InsertAsync(relation);
        }

        public static async Task InsertAsync<TSource>(this IQueryBuffer buffer, IEnumerable<TSource> data, params (string Select, string Insert)[] mappingHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IEnumerable<string> selectHeader = mappingHeader.Select(t => t.Select);
            IEnumerable<string> insertHeader = mappingHeader.Select(t => t.Insert);

            await buffer.InsertAsync(data, selectHeader, insertHeader);
        }

        public static async Task InsertAsync<TSource>(this IQueryBuffer buffer, IEnumerable<TSource> data, IEnumerable<string> selectHeader, IEnumerable<string> insertHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = buffer.Store.From(data).Select(selectHeader);

            await buffer.InsertAsync(relation, insertHeader);
        }
        #endregion
    }
}
