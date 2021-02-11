using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Collections;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.Extensions;
using Jerrycurl.Reflection;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using System.Reflection;
using Jerrycurl.Data.Queries.Internal.IO.Writers;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal abstract class BaseParser
    {
        public ISchema Schema { get; set; }

        public BaseParser(ISchema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        protected bool IsResultNode(Node node) => node.Metadata.HasFlag(BindingMetadataFlags.Model);
        protected bool IsResultListNode(Node node) => (node.Metadata.Parent != null && node.Metadata.Parent.HasFlag(BindingMetadataFlags.Model));

        protected T FindData<T>(Node node, IEnumerable<T> header)
            where T : DataAttribute
        {
            foreach (T attribute in header)
            {
                MetadataIdentity metadata = new MetadataIdentity(node.Metadata.Identity.Schema, attribute.Name);

                if (metadata.Equals(node.Identity))
                    return attribute;
            }

            return null;
        }

        protected virtual DataReader CreateDataReader(BaseResult result, Node node)
        {
            if (node == null)
                return null;

            if (node.Data is ColumnAttribute column)
            {
                ColumnReader reader = new ColumnReader(node)
                {
                    Column = new ColumnMetadata(column.Name, column.Type, column.TypeName, column.Index),
                };

                this.AddHelper(result, reader);

                return reader;
            }
            else if (node.Data is AggregateAttribute aggregate)
            {
                return new AggregateReader(node)
                {
                    Attribute = aggregate,
                };
            }

            return null;
        }

        protected BaseReader CreateReader(BaseResult result, Node node)
        {
            if (node == null)
                return null;
            else if (node.Data != null)
                return this.CreateDataReader(result, node);
            else if (node.Metadata.HasFlag(BindingMetadataFlags.Dynamic))
            {
                return new DynamicReader(node)
                {
                    Properties = node.Properties.Select(n => this.CreateReader(result, n)).ToList(),
                };
            }
            else
            {
                NewReader reader = new NewReader(node.Metadata)
                {
                    Properties = node.Properties.Select(n => this.CreateReader(result, n)).ToList(),
                };

                this.AddPrimaryKey(reader);

                return reader;
            }
        }

        protected virtual void CreateKeys(NewReader reader)
        {
            this.AddPrimaryKey(reader);
        }

        private void AddPrimaryKey(NewReader binder)
        {
            IReferenceMetadata metadata = binder.Metadata.Identity.Lookup<IReferenceMetadata>();
            IEnumerable<IReferenceKey> primaryKeys = metadata?.Keys.Where(k => k.HasFlag(ReferenceKeyFlags.Primary)).ToList();
            IEnumerable<KeyReader> keys = primaryKeys?.Select(k => FindPrimaryKey(binder, k)).ToList();

            binder.PrimaryKey = keys?.NotNull().FirstOrDefault();
        }

        protected KeyReader FindChildKey(NewReader binder, IReference reference) => this.FindKey(binder, reference, reference.FindChildKey());
        protected KeyReader FindParentKey(NewReader binder, IReference reference) => this.FindKey(binder, reference, reference.FindParentKey());
        protected KeyReader FindPrimaryKey(NewReader binder, IReferenceKey primaryKey) => this.FindKey(binder, null, primaryKey);

        private KeyReader FindKey(NewReader binder, IReference reference, IReferenceKey referenceKey)
        {
            if (referenceKey == null)
                return null;

            List<DataReader> values = new List<DataReader>();

            foreach (MetadataIdentity identity in referenceKey.Properties.Select(m => m.Identity))
            {
                DataReader value = binder.Properties.FirstOfType<DataReader>(m => m.Metadata.Identity.Equals(identity));

                values.Add(value);

                if (value != null)
                    value.CanBeDbNull = !referenceKey.HasFlag(ReferenceKeyFlags.Primary);
            }

            if (values.All(v => v != null))
            {
                KeyReader key = new KeyReader()
                {
                    Values = values,
                    Reference = reference,
                };

                if (reference != null)
                {
                    foreach (var (value, keyType) in values.Zip(GetKeyType(reference)))
                        value.KeyType = keyType;

                    key.KeyType = this.GetCompositeKeyType(values.Select(v => v.KeyType));
                    key.Variable = Expression.Variable(key.KeyType, "key");
                }


                return key;
            }

            return null;
        }

        private void AddHelper(BaseResult result, ColumnReader reader)
        {
            IBindingHelperContract helper = reader.Metadata.Helper;

            if (helper != null)
            {
                HelperWriter writer = new HelperWriter(reader.Metadata)
                {
                    Object = helper.Object,
                    BufferIndex = result.Helpers.Count,
                    Variable = Expression.Variable(helper.Type, $"helper_{result.Helpers.Count}"),
                };

                reader.Helper = writer.Variable;

                result.Helpers.Add(writer);
            }
        }


        private static IList<Type> GetKeyType(IReference reference)
        {
            if (reference == null)
                return null;

            List<Type> keyType = new List<Type>();

            foreach (var (left, right) in reference.Key.Properties.Zip(reference.Other.Key.Properties))
            {
                Type leftType = Nullable.GetUnderlyingType(left.Type) ?? left.Type;
                Type rightType = Nullable.GetUnderlyingType(right.Type) ?? right.Type;

                if (leftType != rightType)
                    ThrowInvalidKeyException(reference);

                keyType.Add(leftType);
            }

            return keyType;
        }

        private static void ThrowInvalidKeyException(IReference reference)
        {
            string leftTuple = $"({string.Join(", ", reference.Key.Properties.Select(m => m.Type.GetSanitizedName()))})";
            string rightTuple = $"({string.Join(", ", reference.Other.Key.Properties.Select(m => m.Type.GetSanitizedName()))})";

            throw new InvalidOperationException($"Key types are incompatible. Cannot convert {leftTuple} to {rightTuple}.");
        }

        private Type GetCompositeKeyType(IEnumerable<Type> keyType)
        {
            Type[] typeArray = keyType.ToArray();

            if (typeArray.Length == 0)
                return null;
            else if (typeArray.Length == 1)
                return typeArray[0];
            else if (typeArray.Length == 2)
                return typeof(CompositeKey<,>).MakeGenericType(typeArray[0], typeArray[1]);
            else if (typeArray.Length == 3)
                return typeof(CompositeKey<,,>).MakeGenericType(typeArray[0], typeArray[1], typeArray[2]);
            else if (typeArray.Length == 4)
                return typeof(CompositeKey<,,,>).MakeGenericType(typeArray[0], typeArray[1], typeArray[2], typeArray[3]);
            else
            {
                Type restType = this.GetCompositeKeyType(keyType.Skip(4));

                return typeof(CompositeKey<,,,,>).MakeGenericType(typeArray[0], typeArray[1], typeArray[2], typeArray[3], restType);
            }
        }

    }
}
