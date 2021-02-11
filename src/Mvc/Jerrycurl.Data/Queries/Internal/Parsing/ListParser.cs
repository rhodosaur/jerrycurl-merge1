using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Caching;
using Jerrycurl.Data.Queries.Internal.Extensions;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using Jerrycurl.Data.Queries.Internal.IO.Readers;
using Jerrycurl.Data.Queries.Internal.IO.Writers;
using System.Runtime.CompilerServices;

namespace Jerrycurl.Data.Queries.Internal.Parsing
{
    internal class ListParser : BaseParser
    {
        public BufferCache Buffer { get; }
        public QueryType QueryType { get; }

        public ListParser(BufferCache cache, QueryType queryType)
            : base(cache?.Schema)
        {            
            this.Buffer = cache ?? throw new ArgumentException(nameof(cache));
            this.QueryType = queryType;
        }

        public ListResult Parse(IEnumerable<ColumnAttribute> header)
        {
            NodeTree nodeTree = NodeParser.Parse(this.Schema, header);
            ListResult result = new ListResult(this.Schema, this.QueryType);

            this.CreateJoins(result, nodeTree);
            this.CreateAggregates(result, nodeTree);

            return result;
        }

        private void CreateAggregates(ListResult result, NodeTree nodeTree)
        {
            if (this.QueryType != QueryType.Aggregate)
                return;

            IEnumerable<Node> aggregateNodes = nodeTree.Nodes.Where(IsAggregateNode);

            foreach (Node node in aggregateNodes.Where(n => n.Data != null))
            {
                int bufferIndex = this.Buffer.GetAggregateIndex(node.Identity);

                AggregateWriter writer = new AggregateWriter(node)
                {
                    Attribute = new AggregateAttribute(node.Identity.Name, aggregateIndex: bufferIndex, listIndex: null),
                    Value = this.CreateDataReader(result, node)
                };

                result.Aggregates.Add(writer);
            }

            static bool IsAggregateNode(Node node)
            {
                if (node.Metadata.Relation.Depth == 0)
                    return true;
                else if (node.Metadata.Relation.Depth == 1 && node.Metadata.MemberOf.Parent.HasFlag(BindingMetadataFlags.Model))
                    return true;

                return false;
            }
        }

        private void CreateJoins(ListResult result, NodeTree nodeTree)
        {
            foreach (Node node in nodeTree.Items)
            {
                JoinWriter writer = new JoinWriter(node)
                {
                    Value = this.CreateReader(result, node),
                    Depth = node.Metadata.Relation.Depth,
                };

                this.AddPrimaryKey(writer);
                this.AddChildKey(result, writer);

                result.Joins.Add(writer);
            }

            result.Joins = result.Joins.OrderByDescending(w => w.Depth).ThenByDescending(GetNameDepth).ToList();

            int GetNameDepth(JoinWriter writer) => result.Schema.Notation.Depth(writer.Metadata.Identity.Name);
        }

        protected override BaseReader CreateReader(BaseResult result, Node node)
        {
            BaseReader reader = base.CreateReader(result, node);

            if (reader is NewReader newReader)
                this.AddParentKeys((ListResult)result, newReader);

            return reader;
        }

        private void AddPrimaryKey(JoinWriter writer)
        {
            if (writer.Value is NewReader newValue)
            {
                writer.PrimaryKey = newValue.PrimaryKey;
                newValue.PrimaryKey = null;
            }
        }

        private void AddParentKeys(ListResult result, NewReader reader)
        {
            IEnumerable<IReference> references = this.GetParentReferences(reader.Metadata);
            IEnumerable<KeyReader> joinKeys = references.Select(r => this.FindParentKey(reader, r));

            foreach (KeyReader joinKey in joinKeys.NotNull().DistinctBy(k => k.Reference.Other.Metadata.Identity))
            {
                this.InitializeJoinKey(result, joinKey);

                JoinReader join = new JoinReader()
                {
                    JoinKey = joinKey,
                    Metadata = joinKey.Reference.Other.Metadata.Identity.Require<IBindingMetadata>(),
                };

                reader.JoinKeys.Add(joinKey);
                reader.Properties.Add(join);
            }
        }

        private ListIndex CreateIndex(BaseReader reader)
        {
            return new ListIndex()
            {
                BufferIndex = this.Buffer.GetListIndex(reader.Identity),
            };
        }

        private JoinIndex CreateIndex(KeyReader key, IReference reference)
        {
            return new JoinIndex()
            {
                List = new ListIndex()
                {
                    BufferIndex = this.Buffer.GetParentIndex(reference),
                },
                Buffer = Expression.Variable(typeof(ElasticArray)),
                BufferIndex = this.Buffer.GetChildIndex(reference),
                Key = key,
            };
        }

        private void AddChildKey(ListResult result, JoinWriter writer)
        {
            IEnumerable<IReference> references = this.GetChildReferences(writer.Metadata);

            if (!references.Any())
            {
                KeyReader listKey = new KeyReader(writer.Metadata);

                this.InitializeJoinKey(result, listKey);
            }
            else if (writer.Value is NewReader reader)
            {
                IEnumerable<KeyReader> joinKeys = references.Select(r => this.FindChildKey(reader, r));

                KeyReader joinKey = joinKeys.NotNull().FirstOrDefault();

                if (joinKey != null)
                {
                    this.InitializeJoinKey(result, joinKey);

                    writer.JoinKey = joinKey;
                }
            }

            if (writer.JoinKey == null && references.Any())
            {
                IReference reference = references.First();

                throw BindingException.NoReferenceFound(reference.Metadata.Identity, reference.Other.Metadata.Identity);
            }
        }

        private void InitializeJoinKey(ListResult result, KeyReader joinKey)
        {
            if (joinKey.Reference != null)
            {
                int index = 0;

                foreach (DataReader valueReader in joinKey.Values)
                {
                    valueReader.CanBeDbNull = false;
                    valueReader.IsDbNull ??= Expression.Variable(typeof(bool), $"key_{index++}_isnull");
                    valueReader.Variable ??= Expression.Variable(valueReader.KeyType, $"key_{index}");
                }

                if (joinKey.Reference.HasFlag(ReferenceFlags.Self))
                    joinKey.Reference = this.GetRecursiveReference(joinKey.Metadata);

                this.InitializeKeyTypes(joinKey);

                joinKey.BufferIndex = this.Buffer.GetParentIndex(joinKey.Reference);
                joinKey.List = this.GetListVariable(joinKey);
                joinKey.Array = Expression.Variable(typeof(ElasticArray));
                joinKey.ArrayIndex = this.Buffer.GetChildIndex(joinKey.Reference);
            }
            else
            {
                joinKey.List = this.GetListVariable(joinKey);
                joinKey.BufferIndex = this.Buffer.GetListIndex(joinKey.Metadata.Identity);
            }
            
            this.InitializeList(result, joinKey);
        }

        private void InitializeList(ListResult result, KeyReader joinKey)
        {
            ListWriter writer = result.Lists.FirstOrDefault(w => w.JoinKey.BufferIndex == joinKey.BufferIndex);

            if (writer == null)
            {
                writer = new ListWriter(joinKey.Metadata)
                {
                    JoinKey = joinKey,
                };

                result.Lists.Add(writer);
            }
        }

        private void InitializeKeyTypes(KeyReader joinKey)
        {
            List<Type> keyTypes = new List<Type>();

            foreach (var (left, right) in joinKey.Reference.Key.Properties.Zip(joinKey.Reference.Other.Key.Properties))
            {
                Type leftType = Nullable.GetUnderlyingType(left.Type) ?? left.Type;
                Type rightType = Nullable.GetUnderlyingType(right.Type) ?? right.Type;

                if (leftType != rightType)
                    throw BindingException.IncompatibleReference(joinKey.Reference);

                keyTypes.Add(leftType);
            }

            foreach (var (reader, keyType) in joinKey.Values.Zip(keyTypes))
                reader.KeyType = keyType;

            joinKey.KeyType = this.GetCompositeKeyType(keyTypes);
            joinKey.List = this.GetListVariable(joinKey);
        }

        private ParameterExpression GetListVariable(KeyReader joinKey)
        {
            if (joinKey.Reference == null)
                return Expression.Variable(joinKey.Metadata.Composition.Construct.Type);

            Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(joinKey.KeyType, typeof(ElasticArray));

            return Expression.Variable(dictionaryType);
        }

        private IReference GetRecursiveReference(IBindingMetadata metadata)
        {
            return this.GetChildReferences(metadata).FirstOrDefault().Other; // this aint good enough
        }

        private IEnumerable<IReference> GetParentReferences(IBindingMetadata metadata)
            => this.GetValidReferences(metadata).Where(r => r.HasFlag(ReferenceFlags.Parent));

        private IEnumerable<IReference> GetChildReferences(IBindingMetadata metadata)
            => this.GetValidReferences(metadata).Where(r => r.HasFlag(ReferenceFlags.Child) && !r.HasFlag(ReferenceFlags.Self));

        private IEnumerable<IReference> GetValidReferences(IBindingMetadata metadata)
        {
            IReferenceMetadata referenceMetadata = metadata.Identity.Lookup<IReferenceMetadata>();

            if (referenceMetadata != null)
                return referenceMetadata.References.Where(IsValid).OrderBy(GetPriority);

            return Array.Empty<IReference>();

            bool IsValid(IReference reference)
            {
                if (!reference.HasFlag(ReferenceFlags.Many) && !reference.Other.HasFlag(ReferenceFlags.Many))
                    return false;

                //IReference parentRef = reference.Find(ReferenceFlags.Parent);
                //IReference childRef = reference.Find(ReferenceFlags.Child);

                //if (this.QueryType == QueryType.Aggregate)
                //{
                //    if (parentRef.Metadata.Relation.HasFlag(RelationMetadataFlags.Model))
                //        return false;
                //    else if (parentRef.Metadata.Relation.Parent.HasFlag(RelationMetadataFlags.Model))
                //        return false;
                //}

                return true;
            }

            static int GetPriority(IReference reference)
            {
                if (reference.HasFlag(ReferenceFlags.One | ReferenceFlags.Primary))
                    return 0;
                else if (reference.HasFlag(ReferenceFlags.One | ReferenceFlags.Candidate))
                    return 1;
                else if (reference.HasFlag(ReferenceFlags.Many | ReferenceFlags.Foreign))
                    return 2;
                else
                    return 3;
            }
        }

        private Type GetCompositeKeyType(IEnumerable<Type> keyTypes)
        {
            Type[] typeArray = keyTypes.ToArray();

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
                Type restType = this.GetCompositeKeyType(keyTypes.Skip(4));

                return typeof(CompositeKey<,,,,>).MakeGenericType(typeArray[0], typeArray[1], typeArray[2], typeArray[3], restType);
            }
        }

    }
}
