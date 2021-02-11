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
using Jerrycurl.Data.Queries.Internal.IO.Targets;
using System.IO;

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

            this.CreateLists(result, nodeTree);
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

        private void CreateLists(ListResult result, NodeTree nodeTree)
        {
            foreach (Node node in nodeTree.Items)
            {
                ListWriter writer = new ListWriter(node)
                {
                    Source = this.CreateReader(result, node),
                };

                this.AddPrimaryKey(writer);
                this.AddChildKey(result, writer);

                result.Lists.Add(writer);
            }

            //result.Joins = result.Joins.OrderByDescending(w => w.Depth).ThenByDescending(GetNameDepth).ToList();

            //int GetNameDepth(JoinWriter writer) => result.Schema.Notation.Depth(writer.Metadata.Identity.Name);
        }

        protected override BaseReader CreateReader(BaseResult result, Node node)
        {
            BaseReader reader = base.CreateReader(result, node);

            if (reader is NewReader newReader)
                this.AddParentKeys((ListResult)result, newReader);

            return reader;
        }

        private void AddPrimaryKey(ListWriter writer)
        {
            if (writer.Source is NewReader newReader)
            {
                writer.PrimaryKey = newReader.PrimaryKey;
                newReader.PrimaryKey = null;
            }
        }

        private BaseTarget CreateTarget(ListResult result, BaseReader source, KeyReader joinKey)
        {
            if (joinKey == null)
            {
                int bufferIndex;

                if (source.Metadata.HasFlag(BindingMetadataFlags.Model) || source.Metadata.Parent.HasFlag(BindingMetadataFlags.Model))
                    bufferIndex = this.Buffer.GetResultIndex();
                else
                    bufferIndex = this.Buffer.GetListIndex(source.Identity);

                ListTarget target = result.Targets.FirstOfType<ListTarget>(t => t.BufferIndex == bufferIndex);

                if (target != null)
                    return target;

                target = new ListTarget()
                {
                    BufferIndex = bufferIndex,
                };

                if (source.Metadata.HasFlag(BindingMetadataFlags.Item))
                {
                    target.NewList = new NewReader(source.Metadata.Parent);
                    target.Variable = Expression.Variable(source.Metadata.Parent.Composition.Construct.Type, $"_list{target.BufferIndex}");
                    target.AddMethod = source.Metadata.Parent.Composition.Add;
                }

                result.Targets.Add(target);

                return target;
            }
            else
            {
                Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(joinKey.Variable.Type, typeof(ElasticArray));

                int bufferIndex = this.Buffer.GetParentIndex(joinKey.Reference);
                int joinIndex = this.Buffer.GetChildIndex(joinKey.Reference);

                BaseTarget listTarget = result.Targets.FirstOrDefault(t => t.BufferIndex == bufferIndex);
                JoinTarget target = result.Targets.FirstOfType<JoinTarget>(t => t.BufferIndex == bufferIndex && t.JoinIndex == joinIndex);

                if (target != null)
                    return target;

                target = new JoinTarget()
                {
                    BufferIndex = bufferIndex,
                    Variable = listTarget?.Variable ?? Expression.Variable(dictionaryType, $"_dict{bufferIndex}"),
                    JoinBuffer = Expression.Variable(typeof(ElasticArray), $"_join{bufferIndex}_{joinIndex}"),
                    JoinIndex = this.Buffer.GetChildIndex(joinKey.Reference),
                    Key = joinKey,
                };

                IReference childReference = joinKey.Reference.Find(ReferenceFlags.Child);
                IBindingMetadata childMetadata = childReference.Metadata.Identity.Require<IBindingMetadata>();

                if (childMetadata.HasFlag(BindingMetadataFlags.Item))
                {
                    target.NewList = new NewReader(childMetadata.Parent);
                    target.AddMethod = childMetadata.Parent.Composition.Add;
                }

                result.Targets.Add(target);

                return target;
            }
        }

        private void AddParentKeys(ListResult result, NewReader reader)
        {
            foreach (IReference reference in this.GetParentReferences(reader.Metadata))
            {
                KeyReader joinKey = this.FindParentKey(reader, reference);

                if (joinKey != null)
                {
                    this.InitializeKey(joinKey);

                    JoinReader join = new JoinReader(reference)
                    {
                        Target = (JoinTarget)this.CreateTarget(result, reader, joinKey),
                    };

                    reader.Joins.Add(join.Target);
                    reader.Properties.Add(join);
                }
            }
        }

        private IEnumerable<Type> GetReferenceKeyTypes(IReference reference, bool throwOnInvalid = false)
        {
            IReferenceKey parentKey = reference.FindParentKey();
            IReferenceKey childKey = reference.FindChildKey();

            foreach (var (childValue, parentValue) in childKey.Properties.Zip(parentKey.Properties))
            {
                Type childType = Nullable.GetUnderlyingType(childValue.Type) ?? childValue.Type;
                Type parentType = Nullable.GetUnderlyingType(parentValue.Type) ?? parentValue.Type;

                if (throwOnInvalid && childType != parentType)
                    throw BindingException.IncompatibleReference(reference);

                yield return parentType;
            }
        }

        private void AddChildKey(ListResult result, ListWriter writer)
        {
            IList<IReference> references = this.GetChildReferences(writer.Source.Metadata).ToList();
            KeyReader childKey = references.Select(r => this.FindChildKey(writer.Source, r)).NotNull().FirstOrDefault();

            if (childKey != null)
                this.InitializeKey(childKey, throwOnInvalid: true);

            if (childKey == null && this.RequiresReference(writer))
                throw new InvalidOperationException();

            writer.Target = this.CreateTarget(result, writer.Source, childKey);
        }

        private bool RequiresReference(ListWriter writer)
        {
            return false;
        }

        private void InitializeKey(KeyReader key, bool throwOnInvalid = false)
        {
            foreach (DataReader value in key.Values)
            {
                value.IsDbNull ??= Expression.Variable(typeof(bool), "kv_isnull");
                value.Variable ??= Expression.Variable(value.Metadata.Type, "kv");

                if (key.Reference.HasFlag(ReferenceFlags.Child))
                    value.CanBeDbNull = false;
            }

            if (key.Reference != null)
            {
                IList<Type> keyTypes = this.GetReferenceKeyTypes(key.Reference, throwOnInvalid).ToList();

                foreach (var (value, keyType) in key.Values.Zip(keyTypes))
                    value.KeyType = keyType;

                key.Variable = Expression.Variable(CompositeKey.Create(keyTypes));

                if (key.Reference.HasFlag(ReferenceFlags.Self))
                    key.Reference = this.GetRecursiveReference(key.Reference);
            }

        }

        private IReference GetRecursiveReference(IReference reference)
        {
            return reference; // somehow locate the other reference through reference.Find(Parent).References.HasFlag(Child).Other
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

                IReference parentRef = reference.Find(ReferenceFlags.Parent);
                IReference childRef = reference.Find(ReferenceFlags.Child);

                if (this.QueryType == QueryType.Aggregate)
                {
                    if (parentRef.Metadata.Relation.HasFlag(RelationMetadataFlags.Model))
                        return false;
                    else if (parentRef.Metadata.Relation.Parent.HasFlag(RelationMetadataFlags.Model))
                        return false;
                }

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
    }
}
