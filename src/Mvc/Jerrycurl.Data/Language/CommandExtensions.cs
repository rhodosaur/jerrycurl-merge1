using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jerrycurl.Data.Commands;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Data.Language
{
    public static class CommandExtensions
    {
        #region " Update "
        public static CommandBuffer Update(this CommandBuffer buffer, IRelation relation, params string[] targetHeader)
            => buffer.Update(relation, (IEnumerable<string>)targetHeader);

        public static CommandBuffer Update(this CommandBuffer buffer, IRelation relation, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(relation));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            using IDataReader dataReader = relation.GetDataReader(targetHeader);

            buffer.Update(dataReader);

            return buffer;
        }

        public static CommandBuffer Insert(this CommandBuffer buffer, IRelation relation)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            using IDataReader dataReader = relation.GetDataReader();

            buffer.Update(dataReader);

            return buffer;
        }

        public static CommandBuffer Update<TSource>(this CommandBuffer buffer, TSource source, params string[] sourceHeader)
            => buffer.Update(source, (IEnumerable<string>)sourceHeader);

        public static CommandBuffer Update<TSource>(this CommandBuffer buffer, TSource source, IEnumerable<string> sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            ISchema schema = buffer.Store.GetSchema(typeof(TSource));

            buffer.Update(source, schema.Select(sourceHeader));

            return buffer;
        }

        public static CommandBuffer Update<TSource>(this CommandBuffer buffer, TSource source, RelationHeader sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            IRelation relation = new Relation(buffer.Store.From(source), sourceHeader);

            buffer.Update(relation);

            return buffer;
        }

        public static CommandBuffer Update<TSource>(this CommandBuffer buffer, TSource source, params (string Source, string Target)[] mappingHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            IEnumerable<string> sourceHeader = mappingHeader.Select(t => t.Source);
            IEnumerable<string> targetHeader = mappingHeader.Select(t => t.Target);

            buffer.Update(source, sourceHeader, targetHeader);

            return buffer;
        }

        public static CommandBuffer Update<TSource>(this CommandBuffer buffer, TSource source, IEnumerable<string> sourceHeader, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            IRelation relation = buffer.Store.From(source).Select(sourceHeader);

            buffer.Update(relation, targetHeader);

            return buffer;
        }
        #endregion

        #region " UpdateAsync "
        public static Task<CommandBuffer> UpdateAsync(this CommandBuffer buffer, IRelation relation, params string[] targetHeader)
            => buffer.UpdateAsync(relation, (IEnumerable<string>)targetHeader);

        public static async Task<CommandBuffer> UpdateAsync(this CommandBuffer buffer, IRelation relation, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(relation));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            using DbDataReader dataReader = relation.GetDataReader(targetHeader);

            await buffer.UpdateAsync(dataReader);

            return buffer;
        }

        public static async Task<CommandBuffer> UpdateAsync(this CommandBuffer buffer, IRelation relation)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            using DbDataReader dataReader = relation.GetDataReader();

            await buffer.UpdateAsync(dataReader);

            return buffer;
        }

        public static Task<CommandBuffer> UpdateAsync<TSource>(this CommandBuffer buffer, TSource source, params string[] sourceHeader)
            => buffer.UpdateAsync(source, (IEnumerable<string>)sourceHeader);

        public static async Task<CommandBuffer> UpdateAsync<TSource>(this CommandBuffer buffer, TSource source, IEnumerable<string> sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            ISchema schema = buffer.Store.GetSchema(typeof(TSource));

            await buffer.UpdateAsync(source, schema.Select(sourceHeader));

            return buffer;
        }

        public static async Task<CommandBuffer> UpdateAsync<TSource>(this CommandBuffer buffer, TSource source, RelationHeader sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            IRelation relation = new Relation(buffer.Store.From(source), sourceHeader);

            await buffer.UpdateAsync(relation);

            return buffer;
        }

        public static async Task<CommandBuffer> UpdateAsync<TSource>(this CommandBuffer buffer, TSource source, params (string Source, string Target)[] mappingHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (mappingHeader == null)
                throw new ArgumentNullException(nameof(mappingHeader));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            IEnumerable<string> sourceHeader = mappingHeader.Select(t => t.Source);
            IEnumerable<string> targetHeader = mappingHeader.Select(t => t.Target);

            await buffer.UpdateAsync(source, sourceHeader, targetHeader);

            return buffer;
        }

        public static async Task<CommandBuffer> UpdateAsync<TSource>(this CommandBuffer buffer, TSource source, IEnumerable<string> sourceHeader, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Store == null)
                throw CommandException.NoSchemaStoreAttached();

            IRelation relation = buffer.Store.From(source).Select(sourceHeader);

            await buffer.UpdateAsync(relation, targetHeader);

            return buffer;
        }
        #endregion
    }
}
